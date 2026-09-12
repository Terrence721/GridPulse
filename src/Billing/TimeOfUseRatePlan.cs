namespace GridPulse.Billing;

public sealed class TimeOfUseRatePlan(decimal peakRatePerKwh, decimal offPeakRatePerKwh, TimeOnly peakStart, TimeOnly peakEnd) : IRatePlan
{
    public decimal CalculateCost(IReadOnlyList<HourlyUsageSnapshot> hourlyUsage)
    {
        var cost = 0m;

        foreach (var usage in hourlyUsage)
        {
            var hourOfDay = TimeOnly.FromTimeSpan(usage.PeriodStart.UtcDateTime.TimeOfDay);
            var rate = IsWithinPeakWindow(hourOfDay) ? peakRatePerKwh : offPeakRatePerKwh;

            cost += (decimal)usage.TotalKwh * rate;
        }

        return cost;
    }

    private bool IsWithinPeakWindow(TimeOnly hourOfDay)
    {
        return peakStart <= peakEnd
            ? hourOfDay >= peakStart && hourOfDay < peakEnd
            : hourOfDay >= peakStart || hourOfDay < peakEnd;
    }
}
