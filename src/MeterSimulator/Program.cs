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
    host.Run();
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures)
    {
        Console.Error.WriteLine(failure);
    }

    Environment.Exit(1);
}
