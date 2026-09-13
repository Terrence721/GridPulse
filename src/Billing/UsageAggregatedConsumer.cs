using Confluent.Kafka;
using GridPulse.Billing.Avro;

namespace GridPulse.Billing;

public sealed class UsageAggregatedConsumer(
    IConsumer<string, UsageAggregated> consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<UsageAggregatedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe("usage.aggregated");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, UsageAggregated> result;

            try
            {
                result = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogWarning(ex, "Failed to consume a usage aggregated message");
                continue;
            }

            var usage = result.Message.Value;

            using var scope = scopeFactory.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<InvoiceGenerator>();
            await generator.GenerateAsync(
                usage.AccountId,
                DateTimeOffset.FromUnixTimeMilliseconds(usage.PeriodStartUnixMilliseconds),
                DateTimeOffset.FromUnixTimeMilliseconds(usage.PeriodEndUnixMilliseconds),
                usage.TotalKwh,
                stoppingToken);
        }
    }
}
