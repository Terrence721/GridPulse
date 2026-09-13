using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using GridPulse.UsageAggregation.Avro;

namespace GridPulse.UsageAggregation;

public sealed class ReadingProcessor(UsageAggregationDbContext dbContext, IProducer<string, UsageAggregated> producer)
{
    public async Task<bool> ProcessAsync(MeterReadingRequest request, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await dbContext.ProcessedReadings
            .AnyAsync(r => r.ReadingId == request.ReadingId, cancellationToken);

        if (alreadyProcessed)
        {
            return false;
        }

        dbContext.ProcessedReadings.Add(new ProcessedReading
        {
            ReadingId = request.ReadingId,
            MeterId = request.MeterId,
            Timestamp = request.Timestamp,
            Kwh = request.Kwh,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        var periodStart = new DateTimeOffset(
            request.Timestamp.Year, request.Timestamp.Month, request.Timestamp.Day, request.Timestamp.Hour, 0, 0,
            request.Timestamp.Offset);
        var periodEnd = periodStart.AddHours(1);

        var hourlyUsage = await dbContext.HourlyUsages
            .FirstOrDefaultAsync(u => u.MeterId == request.MeterId && u.PeriodStart == periodStart, cancellationToken);

        if (hourlyUsage is null)
        {
            hourlyUsage = new HourlyUsage
            {
                Id = Guid.NewGuid(),
                MeterId = request.MeterId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                TotalKwh = 0
            };
            dbContext.HourlyUsages.Add(hourlyUsage);
        }

        hourlyUsage.TotalKwh += request.Kwh;

        await dbContext.SaveChangesAsync(cancellationToken);

        await producer.ProduceAsync("usage.aggregated", new Message<string, UsageAggregated>
        {
            Key = hourlyUsage.MeterId,
            Value = new UsageAggregated
            {
                MeterId = hourlyUsage.MeterId,
                PeriodStartUnixMilliseconds = hourlyUsage.PeriodStart.ToUnixTimeMilliseconds(),
                PeriodEndUnixMilliseconds = hourlyUsage.PeriodEnd.ToUnixTimeMilliseconds(),
                TotalKwh = hourlyUsage.TotalKwh
            }
        }, cancellationToken);

        return true;
    }
}
