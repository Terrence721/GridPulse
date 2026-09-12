namespace GridPulse.Billing;

public sealed class TieredRatePlan(IReadOnlyList<RateTier> tiers) : IRatePlan
{
    public decimal CalculateCost(IReadOnlyList<HourlyUsageSnapshot> hourlyUsage)
    {
        var remainingKwh = hourlyUsage.Sum(u => u.TotalKwh);
        var previousThreshold = 0.0;
        var cost = 0m;

        foreach (var tier in tiers)
        {
            var tierCapacity = tier.UpToKwh - previousThreshold;
            var kwhInTier = Math.Min(remainingKwh, tierCapacity);

            if (kwhInTier <= 0)
            {
                break;
            }

            cost += (decimal)kwhInTier * tier.PricePerKwh;
            remainingKwh -= kwhInTier;
            previousThreshold = tier.UpToKwh;
        }

        return cost;
    }
}
