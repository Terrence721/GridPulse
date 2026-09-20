using GridPulse.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddValidatedOptions<SmokeTestClientOptions>(SmokeTestClientOptions.SectionName);

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

if (!app.Services.TryValidateStartupOptions<SmokeTestClientOptions>())
{
    return 1;
}

app.MapDefaultEndpoints();
app.Run();
return 0;