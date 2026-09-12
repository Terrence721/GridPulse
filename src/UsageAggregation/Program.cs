using GridPulse.UsageAggregation;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<UsageAggregationDbContext>("gridpulsedb");

var app = builder.Build();
app.MapDefaultEndpoints();

app.Run();
