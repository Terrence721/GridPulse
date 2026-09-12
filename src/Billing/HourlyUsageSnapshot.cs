namespace GridPulse.Billing;

public sealed record HourlyUsageSnapshot(string MeterId, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, double TotalKwh);
