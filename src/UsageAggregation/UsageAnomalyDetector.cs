using Microsoft.Extensions.Options;

namespace GridPulse.UsageAggregation;

public sealed class UsageAnomalyDetector(
    IServiceScopeFactory scopeFactory,
    IOptions<UsageAnomalyOptions> options,
    ILogger<UsageAnomalyDetector> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.SweepIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<UsageAnomalyProcessor>();
                    await processor.DetectAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to run a usage anomaly detection sweep");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
