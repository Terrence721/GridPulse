namespace GridPulse.Billing.Tests;

internal sealed class FakeBillingInvoiceGeneratedPublisher : IBillingInvoiceGeneratedPublisher
{
    public Task PublishAsync(Invoice invoice, CancellationToken cancellationToken) => Task.CompletedTask;
}
