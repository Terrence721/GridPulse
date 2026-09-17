namespace GridPulse.UsageAggregation.Tests;

internal sealed class FakeUsageAnomalyPublisher : IUsageAnomalyPublisher
{
    public List<ProcessedReading> PublishedReadings { get; } = [];

    public Task PublishAsync(ProcessedReading lastReading, CancellationToken cancellationToken)
    {
        PublishedReadings.Add(lastReading);
        return Task.CompletedTask;
    }
}
