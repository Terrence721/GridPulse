using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridPulse.UsageAggregation;

public sealed class UsageAnomalyProcessor(
    UsageAggregationDbContext dbContext,
    IUsageAnomalyPublisher publisher,
    IOptions<UsageAnomalyOptions> options)
{
    public async Task DetectAsync(CancellationToken cancellationToken)
    {
        var staleSince = DateTimeOffset.UtcNow.AddSeconds(
            -(options.Value.ExpectedReadingIntervalSeconds * options.Value.MissedIntervalMultiplier));

        var staleReadings = await dbContext.ProcessedReadings
            .GroupBy(r => r.MeterId)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .Where(r => r.Timestamp < staleSince)
            .ToListAsync(cancellationToken);

        foreach (var reading in staleReadings)
        {
            await publisher.PublishAsync(reading, cancellationToken);
        }
    }
}
