namespace GridPulse.Billing.Tests;

public sealed class FlatRateRatePlanTests
{
    [Fact]
    public void CalculateCost_SingleReading_MultipliesKwhByRate()
    {
        var ratePlan = new FlatRateRatePlan(ratePerKwh: 0.16m);
        var usage = new List<HourlyUsageSnapshot>
        {
            new("MTR-100-Elm St", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), 10.0)
        };

        var cost = ratePlan.CalculateCost(usage);

        Assert.Equal(1.6m, cost);
    }

    [Fact]
    public void CalculateCost_MultipleReadings_SumsBeforeApplyingRate()
    {
        var ratePlan = new FlatRateRatePlan(ratePerKwh: 0.16m);
        var usage = new List<HourlyUsageSnapshot>
        {
            new("MTR-100-Elm St", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), 10.0),
            new("MTR-100-Elm St", DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2), 5.0)
        };

        var cost = ratePlan.CalculateCost(usage);

        Assert.Equal(2.4m, cost);
    }

    [Fact]
    public void CalculateCost_NoUsage_ReturnsZero()
    {
        var ratePlan = new FlatRateRatePlan(ratePerKwh: 0.16m);

        var cost = ratePlan.CalculateCost([]);

        Assert.Equal(0m, cost);
    }
}
