namespace GridPulse.Billing;

public interface IBillingInvoiceGeneratedPublisher
{
    Task PublishAsync(Invoice invoice, CancellationToken cancellationToken);
}
