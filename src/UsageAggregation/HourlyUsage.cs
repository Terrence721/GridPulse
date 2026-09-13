namespace GridPulse.UsageAggregation;

public sealed class HourlyUsage
{
    public Guid Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public double TotalKwh { get; set; }
}
