namespace GridPulse.UsageAggregation;

public sealed record MeterReadingRequest(string MeterId, string AccountId, DateTimeOffset Timestamp, double Kwh, Guid ReadingId);
