using GridPulse.UsageAggregation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<UsageAggregationDbContext>("gridpulsedb");
builder.Services.AddScoped<ReadingProcessor>();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapPost("/readings", async (MeterReadingRequest request, ReadingProcessor processor, CancellationToken cancellationToken) =>
{
    var processed = await processor.ProcessAsync(request, cancellationToken);
    return processed
        ? Results.Accepted(value: new { status = "processed" })
        : Results.Ok(new { status = "duplicate" });
});

app.MapGet("/usage/{meterId}", async (string meterId, DateTimeOffset periodStart, DateTimeOffset periodEnd, UsageAggregationDbContext dbContext, CancellationToken cancellationToken) =>
{
    var hourlyUsage = await dbContext.HourlyUsages
        .Where(u => u.MeterId == meterId && u.PeriodStart >= periodStart && u.PeriodStart < periodEnd)
        .OrderBy(u => u.PeriodStart)
        .Select(u => new HourlyUsageResponse(u.MeterId, u.PeriodStart, u.PeriodEnd, u.TotalKwh))
        .ToListAsync(cancellationToken);

    return Results.Ok(hourlyUsage);
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UsageAggregationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
