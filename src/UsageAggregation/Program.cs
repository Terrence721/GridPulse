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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UsageAggregationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
