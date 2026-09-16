namespace GridPulse.Billing.Tests;

internal sealed class FakeStripeCheckoutSessionCreator : IStripeCheckoutSessionCreator
{
    public Task<StripeCheckoutSession> CreateAsync(Invoice invoice, CancellationToken cancellationToken) =>
        Task.FromResult(new StripeCheckoutSession($"cs_test_{invoice.Id}", $"https://checkout.stripe.com/pay/cs_test_{invoice.Id}"));
}
