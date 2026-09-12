namespace GridPulse.UsageAggregation;

public sealed class ProcessedReading
{
    public Guid ReadingId { get; set; }
    public string MeterId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public double Kwh { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}
