using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using GridPulse.MeterSimulator;
using GridPulse.MeterSimulator.Avro;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.Configure<MeterSimulatorOptions>(
    builder.Configuration.GetSection(MeterSimulatorOptions.SectionName));
builder.Services.AddOptions<MeterSimulatorOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<MeterReadingGenerator>();
builder.Services.AddSingleton<CityBlockMeterIdFactory>();
builder.Services.AddSingleton<CensusAddressValidator>();
builder.Services.AddSingleton<AccountResolver>();
builder.Services.AddSingleton<AccountLookupCache>();
builder.Services.AddSingleton<HouseholdPowerDataset>();
builder.Services.AddSingleton<ISchemaRegistryClient>(_ =>
{
    var schemaRegistryUrl = builder.Configuration["services:schema-registry:http:0"]
        ?? throw new InvalidOperationException("Schema registry endpoint not configured.");
    return new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl });
});
builder.AddKafkaProducer<string, MeterReadingRaw>("kafka", (sp, producerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    producerBuilder.SetValueSerializer(new AvroSerializer<MeterReadingRaw>(schemaRegistry).AsSyncOverAsync());
});
builder.Services.AddHttpClient("census-geocoder", client =>
{
    client.BaseAddress = new Uri("https://geocoding.geo.census.gov/geocoder/");
});
builder.Services.AddHttpClient("account-customer", client =>
{
    client.BaseAddress = new Uri("http://account-customer");
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

MeterSimulatorOptions options;

try
{
    // Trigger validation here, before host.Run() starts the hosting pipeline —
    // resolving it inside host.Run() means the Generic Host's own StartAsync()
    // logs the exception (with a full stack trace) before this catch ever runs.
    options = host.Services.GetRequiredService<IOptions<MeterSimulatorOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

var addressValidator = host.Services.GetRequiredService<CensusAddressValidator>();
var configuredAddress = $"{options.StartingAddress} {options.StreetName}, {options.City}, MI {options.ZipCode}";

bool isRealAddress;
try
{
    isRealAddress = await addressValidator.IsRealAddressAsync(options, CancellationToken.None);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Could not verify '{configuredAddress}' against the Census geocoding API: {ex.Message}");
    return 1;
}

if (!isRealAddress)
{
    Console.Error.WriteLine($"'{configuredAddress}' is not a recognized real-world address. Check StreetName/StartingAddress/City/ZipCode for this deployment.");
    return 1;
}

var meterIdFactory = host.Services.GetRequiredService<CityBlockMeterIdFactory>();
var meterIds = meterIdFactory.Create(options);
var accountResolver = host.Services.GetRequiredService<AccountResolver>();

try
{
    var accountIdsByMeterId = await accountResolver.ResolveAccountIdsAsync(meterIds, CancellationToken.None);
    host.Services.GetRequiredService<AccountLookupCache>().Populate(accountIdsByMeterId);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Could not resolve accountId for a configured meter via Account/Customer Service: {ex.Message}");
    return 1;
}

await host.RunAsync();
return 0;
