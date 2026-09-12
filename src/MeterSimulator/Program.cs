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
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

try
{
    // Trigger validation here, before host.Run() starts the hosting pipeline —
    // resolving it inside host.Run() means the Generic Host's own StartAsync()
    // logs the exception (with a full stack trace) before this catch ever runs.
    _ = host.Services.GetRequiredService<IOptions<MeterSimulatorOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures)
    {
        Console.Error.WriteLine(failure);
    }

    Environment.Exit(1);
}

host.Run();
