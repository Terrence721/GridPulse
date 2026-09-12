namespace GridPulse.UsageAggregation;

public sealed record HourlyUsageResponse(string MeterId, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, double TotalKwh);
