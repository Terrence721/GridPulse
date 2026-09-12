namespace GridPulse.Billing;

public sealed class Invoice
{
    public Guid Id { get; set; }
    public required string MeterId { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public double TotalKwh { get; set; }
    public decimal AmountDue { get; set; }
    public required string RatePlanType { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}
