using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GridPulse.Billing;

public sealed class StripeCheckoutSessionCreator(IStripeClient stripeClient, IOptions<StripeOptions> options) : IStripeCheckoutSessionCreator
{
    public async Task<StripeCheckoutSession> CreateAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var invoiceId = invoice.Id.ToString();
        var amountInCents = (long)Math.Round(invoice.AmountDue * 100m, MidpointRounding.AwayFromZero);

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = invoice.AccountId,
            SuccessUrl = options.Value.SuccessUrl,
            CancelUrl = options.Value.CancelUrl,
            Metadata = new Dictionary<string, string> { ["invoiceId"] = invoiceId },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string> { ["invoiceId"] = invoiceId }
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = options.Value.Currency,
                        UnitAmount = amountInCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"GridPulse invoice {invoiceId}"
                        }
                    }
                }
            ]
        };

        var requestOptions = new RequestOptions { IdempotencyKey = invoiceId };
        var service = new SessionService(stripeClient);
        var session = await service.CreateAsync(sessionOptions, requestOptions, cancellationToken);

        return new StripeCheckoutSession(session.Id, session.Url);
    }
}
