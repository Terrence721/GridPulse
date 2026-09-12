using Microsoft.Extensions.Options;
using GridPulse.MeterSimulator;

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
builder.Services.AddHttpClient("usage-aggregation", client =>
{
    client.BaseAddress = new Uri("http://usage-aggregation");
});
builder.Services.AddHttpClient("census-geocoder", client =>
{
    client.BaseAddress = new Uri("https://geocoding.geo.census.gov/geocoder/");
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

await host.RunAsync();
return 0;
