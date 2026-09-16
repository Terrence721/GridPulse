namespace GridPulse.Billing;

public interface IStripeCheckoutSessionCreator
{
    Task<StripeCheckoutSession> CreateAsync(Invoice invoice, CancellationToken cancellationToken);
}

public sealed record StripeCheckoutSession(string SessionId, string Url);
