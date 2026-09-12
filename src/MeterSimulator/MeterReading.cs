namespace GridPulse.MeterSimulator;

public sealed record MeterReading(string MeterId, DateTimeOffset Timestamp, double Kwh, Guid ReadingId);
