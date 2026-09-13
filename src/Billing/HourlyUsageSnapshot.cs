namespace GridPulse.Billing;

public sealed record HourlyUsageSnapshot(string AccountId, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, double TotalKwh);
