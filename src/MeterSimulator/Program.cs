using GridPulse.MeterSimulator;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.Configure<MeterSimulatorOptions>(
    builder.Configuration.GetSection(MeterSimulatorOptions.SectionName));
builder.Services.AddSingleton<MeterReadingGenerator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
