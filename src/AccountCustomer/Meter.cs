namespace GridPulse.AccountCustomer;

public sealed class Meter
{
    public required string MeterId { get; set; }
    public Guid AccountId { get; set; }
}
