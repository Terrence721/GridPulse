namespace GridPulse.Billing.Tests;

public sealed class TimeOfUseRatePlanTests
{
    private static HourlyUsageSnapshot UsageAt(int hour, double kwh)
    {
        var periodStart = new DateTimeOffset(2026, 9, 12, hour, 0, 0, TimeSpan.Zero);
        return new HourlyUsageSnapshot("MTR-100-Elm St", periodStart, periodStart.AddHours(1), kwh);
    }

    [Fact]
    public void CalculateCost_UsageDuringPeakWindow_ChargesPeakRate()
    {
        var ratePlan = new TimeOfUseRatePlan(
            peakRatePerKwh: 0.22m, offPeakRatePerKwh: 0.10m,
            peakStart: new TimeOnly(14, 0), peakEnd: new TimeOnly(19, 0));

        var cost = ratePlan.CalculateCost([UsageAt(hour: 17, kwh: 10)]);

        Assert.Equal(2.2m, cost);
    }

    [Fact]
    public void CalculateCost_UsageOutsidePeakWindow_ChargesOffPeakRate()
    {
        var ratePlan = new TimeOfUseRatePlan(
            peakRatePerKwh: 0.22m, offPeakRatePerKwh: 0.10m,
            peakStart: new TimeOnly(14, 0), peakEnd: new TimeOnly(19, 0));

        var cost = ratePlan.CalculateCost([UsageAt(hour: 3, kwh: 10)]);

        Assert.Equal(1.0m, cost);
    }

    [Fact]
    public void CalculateCost_MixOfPeakAndOffPeakHours_ChargesEachAtItsOwnRate()
    {
        var ratePlan = new TimeOfUseRatePlan(
            peakRatePerKwh: 0.22m, offPeakRatePerKwh: 0.10m,
            peakStart: new TimeOnly(14, 0), peakEnd: new TimeOnly(19, 0));

        var cost = ratePlan.CalculateCost([UsageAt(hour: 17, kwh: 10), UsageAt(hour: 3, kwh: 10)]);

        Assert.Equal(3.2m, cost);
    }

    [Fact]
    public void CalculateCost_PeakWindowWrapsPastMidnight_ClassifiesHoursCorrectly()
    {
        var ratePlan = new TimeOfUseRatePlan(
            peakRatePerKwh: 0.22m, offPeakRatePerKwh: 0.10m,
            peakStart: new TimeOnly(22, 0), peakEnd: new TimeOnly(6, 0));

        var cost = ratePlan.CalculateCost(
        [
            UsageAt(hour: 23, kwh: 10), // peak: after 22:00
            UsageAt(hour: 3, kwh: 10),  // peak: before 06:00
            UsageAt(hour: 12, kwh: 10)  // off-peak: midday
        ]);

        Assert.Equal(5.4m, cost);
    }
}
