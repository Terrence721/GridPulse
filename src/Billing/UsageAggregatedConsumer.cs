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

            try
            {
                await generator.GenerateAsync(
                    usage.AccountId,
                    DateTimeOffset.FromUnixTimeMilliseconds(usage.PeriodStartUnixMilliseconds),
                    DateTimeOffset.FromUnixTimeMilliseconds(usage.PeriodEndUnixMilliseconds),
                    usage.TotalKwh,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                // A failure here (transient DB error, bad config, etc.) must never crash this
                // BackgroundService - .NET's default BackgroundServiceExceptionBehavior is
                // StopHost, which would take down the entire Billing process (including
                // InvoicePaymentConsumer) as collateral damage over one bad usage message.
                logger.LogWarning(ex, "Failed to generate invoice for account {AccountId}", usage.AccountId);
            }
        }
    }
}
