using Confluent.Kafka;
using GridPulse.Billing.Avro;

namespace GridPulse.Billing;

public sealed class InvoicePaymentConsumer(
    IConsumer<string, BillingInvoiceGenerated> consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<InvoicePaymentConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe("billing.invoice.generated");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, BillingInvoiceGenerated> result;

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
                logger.LogWarning(ex, "Failed to consume a billing invoice generated message");
                continue;
            }

            var invoiceId = Guid.Parse(result.Message.Value.InvoiceId);

            using var scope = scopeFactory.CreateScope();
            var initiator = scope.ServiceProvider.GetRequiredService<InvoicePaymentInitiator>();

            try
            {
                await initiator.InitiateAsync(invoiceId, stoppingToken);
            }
            catch (Exception ex)
            {
                // A failure here (Stripe API error, transient network issue, etc.) must never
                // crash this BackgroundService - .NET's default BackgroundServiceExceptionBehavior
                // is StopHost, which would take down the entire Billing process (including
                // UsageAggregatedConsumer) as collateral damage over one bad invoice.
                logger.LogWarning(ex, "Failed to initiate Stripe payment for invoice {InvoiceId}", invoiceId);
            }
        }
    }
}
