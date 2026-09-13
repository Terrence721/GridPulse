using Confluent.Kafka;
using GridPulse.UsageAggregation.Avro;

namespace GridPulse.UsageAggregation;

public sealed class UsageAggregatedPublisher(IProducer<string, UsageAggregated> producer) : IUsageAggregatedPublisher
{
    public Task PublishAsync(HourlyUsage hourlyUsage, CancellationToken cancellationToken) =>
        producer.ProduceAsync("usage.aggregated", new Message<string, UsageAggregated>
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
}
