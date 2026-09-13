namespace GridPulse.UsageAggregation.Tests;

internal sealed class FakeUsageAggregatedPublisher : IUsageAggregatedPublisher
{
    public Task PublishAsync(HourlyUsage hourlyUsage, CancellationToken cancellationToken) => Task.CompletedTask;
}
