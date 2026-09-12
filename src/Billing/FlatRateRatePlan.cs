namespace GridPulse.Billing;

public sealed class FlatRateRatePlan(decimal ratePerKwh) : IRatePlan
{
    public decimal CalculateCost(IReadOnlyList<HourlyUsageSnapshot> hourlyUsage)
    {
        var totalKwh = hourlyUsage.Sum(u => u.TotalKwh);
        return (decimal)totalKwh * ratePerKwh;
    }
}
