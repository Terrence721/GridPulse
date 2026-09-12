namespace GridPulse.UsageAggregation;

public sealed record MeterReadingRequest(string MeterId, DateTimeOffset Timestamp, double Kwh, Guid ReadingId);
