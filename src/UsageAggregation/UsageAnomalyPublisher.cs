using Confluent.Kafka;
using GridPulse.UsageAggregation.Avro;

namespace GridPulse.UsageAggregation;

public sealed class UsageAnomalyPublisher(IProducer<string, UsageAnomalyDetected> producer) : IUsageAnomalyPublisher
{
    public Task PublishAsync(ProcessedReading lastReading, CancellationToken cancellationToken) =>
        producer.ProduceAsync("usage.anomaly.detected", new Message<string, UsageAnomalyDetected>
        {
            Key = lastReading.MeterId,
            Value = new UsageAnomalyDetected
            {
                MeterId = lastReading.MeterId,
                AccountId = lastReading.AccountId,
                LastSeenAtUnixMilliseconds = lastReading.Timestamp.ToUnixTimeMilliseconds()
            }
        }, cancellationToken);
}
