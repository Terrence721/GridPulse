using Microsoft.EntityFrameworkCore;

namespace GridPulse.Billing;

public sealed class InvoicePaymentInitiator(
    BillingDbContext dbContext,
    IStripeCheckoutSessionCreator checkoutSessionCreator,
    ILogger<InvoicePaymentInitiator> logger)
{
    public async Task InitiateAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            logger.LogWarning("No Invoice found for {InvoiceId} - skipping Stripe checkout session creation", invoiceId);
            return;
        }

        // A Checkout Session already exists for this invoice - known limitation: if
        // AmountDue changes via a later upsert, this pending session goes stale rather
        // than being recreated. Not solved this pass; see docs/architecture.md.
        if (invoice.StripeCheckoutSessionId is not null)
        {
            return;
        }

        // Stripe requires a minimum chargeable amount ($0.50 USD) per Checkout Session. Real
        // hourly usage can legitimately produce invoices below this (a fraction of a kWh) -
        // skip Stripe collection entirely for these rather than let every attempt fail. Not
        // solved further this pass (e.g. accumulating tiny amounts across periods); flagged
        // as a known limitation, see docs/architecture.md.
        if (invoice.AmountDue < 0.50m)
        {
            logger.LogWarning(
                "Invoice {InvoiceId} amount {AmountDue:C} is below Stripe's $0.50 minimum - skipping checkout session creation",
                invoiceId, invoice.AmountDue);
            return;
        }

        var session = await checkoutSessionCreator.CreateAsync(invoice, cancellationToken);
        invoice.StripeCheckoutSessionId = session.SessionId;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created Stripe checkout session {SessionId} for invoice {InvoiceId}: {Url}",
            session.SessionId, invoiceId, session.Url);
    }
}
