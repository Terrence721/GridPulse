using GridPulse.UsageAggregation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<UsageAggregationDbContext>("gridpulsedb");

var app = builder.Build();
app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UsageAggregationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
