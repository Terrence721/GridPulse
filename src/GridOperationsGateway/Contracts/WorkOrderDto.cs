namespace GridPulse.GridOperationsGateway.Contracts;

public sealed record WorkOrderDto
{
    public Guid Id { get; init; }
    public required string HazardType { get; init; }
    public string? MeterId { get; init; }
    public string? AccountId { get; init; }
    public string? StreetName { get; init; }
    public int? StreetNumber { get; init; }
    public required string Description { get; init; }
    public string? AssignedCrew { get; init; }
    public required string Status { get; init; }
    public Guid? OutageId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
