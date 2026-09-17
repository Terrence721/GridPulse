namespace GridPulse.AccountCustomer;

public sealed class Meter
{
    public required string MeterId { get; set; }
    public Guid AccountId { get; set; }
    public required string StreetName { get; set; }
    public int StreetNumber { get; set; }
}
