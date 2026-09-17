namespace GridPulse.GridOperations;

public sealed class Outage
{
    public Guid Id { get; set; }
    public required string StreetName { get; set; }
    public int StreetNumberRangeStart { get; set; }
    public int StreetNumberRangeEnd { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
    public string Status { get; set; } = "Suspected";
}
