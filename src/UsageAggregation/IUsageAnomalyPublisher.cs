namespace GridPulse.UsageAggregation;

public interface IUsageAnomalyPublisher
{
    Task PublishAsync(ProcessedReading lastReading, CancellationToken cancellationToken);
}
