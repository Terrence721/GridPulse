using Confluent.Kafka;
using GridPulse.Billing.Avro;

namespace GridPulse.Billing;

public sealed class BillingInvoiceGeneratedPublisher(IProducer<string, BillingInvoiceGenerated> producer) : IBillingInvoiceGeneratedPublisher
{
    public Task PublishAsync(Invoice invoice, CancellationToken cancellationToken) =>
        producer.ProduceAsync("billing.invoice.generated", new Message<string, BillingInvoiceGenerated>
        {
            Key = invoice.AccountId,
            Value = new BillingInvoiceGenerated
            {
                InvoiceId = invoice.Id.ToString(),
                AccountId = invoice.AccountId,
                AmountDue = (double)invoice.AmountDue,
                DueDateUnixMilliseconds = invoice.DueDate.ToUnixTimeMilliseconds()
            }
        }, cancellationToken);
}
