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

        var staleMeters = await dbContext.ProcessedReadings
            .GroupBy(r => r.MeterId)
            .Select(g => new { MeterId = g.Key, LastSeenAt = g.Max(r => r.Timestamp) })
            .Where(g => g.LastSeenAt < staleSince)
            .ToListAsync(cancellationToken);

        foreach (var stale in staleMeters)
        {
            var reading = await dbContext.ProcessedReadings
                .FirstAsync(r => r.MeterId == stale.MeterId && r.Timestamp == stale.LastSeenAt, cancellationToken);
            await publisher.PublishAsync(reading, cancellationToken);
        }
    }
}
