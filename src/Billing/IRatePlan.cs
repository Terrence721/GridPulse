namespace GridPulse.Billing;

public interface IRatePlan
{
    decimal CalculateCost(IReadOnlyList<HourlyUsageSnapshot> hourlyUsage);
}
