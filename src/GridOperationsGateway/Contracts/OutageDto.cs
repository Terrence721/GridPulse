namespace GridPulse.GridOperationsGateway.Contracts;

public sealed record OutageDto
{
    public Guid Id { get; init; }
    public required string StreetName { get; init; }
    public int StreetNumberRangeStart { get; init; }
    public int StreetNumberRangeEnd { get; init; }
    public DateTimeOffset DetectedAt { get; init; }
    public required string Status { get; init; }
}
