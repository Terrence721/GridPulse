namespace GridPulse.AccountCustomer;

public sealed class Account
{
    public Guid Id { get; set; }
    public required string ContactWebhookUrl { get; set; }
}
