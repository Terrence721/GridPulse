namespace GridPulse.UsageAggregation;

public interface IUsageAggregatedPublisher
{
    Task PublishAsync(HourlyUsage hourlyUsage, CancellationToken cancellationToken);
}
