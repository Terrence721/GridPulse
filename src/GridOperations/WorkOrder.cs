namespace GridPulse.GridOperations;

public sealed class WorkOrder
{
    public Guid Id { get; set; }
    public required string HazardType { get; set; }
    public string? MeterId { get; set; }
    public string? AccountId { get; set; }
    public string? StreetName { get; set; }
    public int? StreetNumber { get; set; }
    public required string Description { get; set; }
    public string? AssignedCrew { get; set; }
    public string Status { get; set; } = "Reported";
    public Guid? OutageId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
