namespace GridPulse.Billing.Tests;

public sealed class TieredRatePlanTests
{
    private static readonly IReadOnlyList<RateTier> Tiers =
    [
        new RateTier(UpToKwh: 500, PricePerKwh: 0.14m),
        new RateTier(UpToKwh: 1000, PricePerKwh: 0.17m),
        new RateTier(UpToKwh: 100000, PricePerKwh: 0.21m)
    ];

    private static IReadOnlyList<HourlyUsageSnapshot> UsageOf(double totalKwh) =>
    [
        new("MTR-100-Elm St", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), totalKwh)
    ];

    [Fact]
    public void CalculateCost_WithinFirstTier_ChargesFirstTierRateOnly()
    {
        var ratePlan = new TieredRatePlan(Tiers);

        var cost = ratePlan.CalculateCost(UsageOf(300));

        Assert.Equal(42m, cost);
    }

    [Fact]
    public void CalculateCost_ExactlyAtTierBoundary_DoesNotSpillIntoNextTier()
    {
        var ratePlan = new TieredRatePlan(Tiers);

        var cost = ratePlan.CalculateCost(UsageOf(500));

        Assert.Equal(70m, cost);
    }

    [Fact]
    public void CalculateCost_SpanningTwoTiers_ChargesEachPortionAtItsOwnRate()
    {
        var ratePlan = new TieredRatePlan(Tiers);

        var cost = ratePlan.CalculateCost(UsageOf(700));

        Assert.Equal(104m, cost);
    }

    [Fact]
    public void CalculateCost_SpanningAllThreeTiers_ChargesEachPortionAtItsOwnRate()
    {
        var ratePlan = new TieredRatePlan(Tiers);

        var cost = ratePlan.CalculateCost(UsageOf(1200));

        Assert.Equal(197m, cost);
    }

    [Fact]
    public void CalculateCost_NoUsage_ReturnsZero()
    {
        var ratePlan = new TieredRatePlan(Tiers);

        var cost = ratePlan.CalculateCost(UsageOf(0));

        Assert.Equal(0m, cost);
    }
}
