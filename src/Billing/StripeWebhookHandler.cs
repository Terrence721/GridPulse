using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GridPulse.Billing;

public sealed class StripeWebhookHandler(BillingDbContext dbContext, IOptions<StripeOptions> options, ILogger<StripeWebhookHandler> logger)
{
    public async Task<bool> HandleAsync(string json, string signatureHeader, CancellationToken cancellationToken)
    {
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, options.Value.WebhookSigningSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Rejected a Stripe webhook with an invalid signature");
            return false;
        }

        var invoiceId = ExtractInvoiceId(stripeEvent);
        if (invoiceId is null)
        {
            logger.LogWarning("Stripe event {EventType} ({EventId}) had no invoiceId in its metadata - ignoring", stripeEvent.Type, stripeEvent.Id);
            return true;
        }

        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            logger.LogWarning("Stripe event {EventType} ({EventId}) referenced unknown invoice {InvoiceId}", stripeEvent.Type, stripeEvent.Id, invoiceId);
            return true;
        }

        // Guarded once, before branching on event type: Stripe redelivers at-least-once and
        // unordered across event types - a late-redelivered failure event must never flip an
        // already-paid invoice back.
        if (invoice.Status == "Paid")
        {
            return true;
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                var session = (Session)stripeEvent.Data.Object;
                invoice.Status = "Paid";
                invoice.PaidAt = DateTimeOffset.UtcNow;
                invoice.StripePaymentIntentId = session.PaymentIntentId;
                break;

            case "payment_intent.payment_failed":
                invoice.Status = "PaymentFailed";
                break;

            default:
                return true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Guid? ExtractInvoiceId(Event stripeEvent)
    {
        var metadata = stripeEvent.Data.Object switch
        {
            Session session => session.Metadata,
            PaymentIntent paymentIntent => paymentIntent.Metadata,
            _ => null
        };

        if (metadata is null || !metadata.TryGetValue("invoiceId", out var invoiceIdText))
        {
            return null;
        }

        return Guid.TryParse(invoiceIdText, out var invoiceId) ? invoiceId : null;
    }
}
