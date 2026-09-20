using GridPulse.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

app.MapDefaultEndpoints();
app.Run();