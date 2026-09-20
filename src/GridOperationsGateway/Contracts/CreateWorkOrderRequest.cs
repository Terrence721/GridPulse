namespace GridPulse.GridOperationsGateway.Contracts;

public sealed record CreateWorkOrderRequest(
    string HazardType,
    string? MeterId,
    string? AccountId,
    string? StreetName,
    int? StreetNumber,
    string Description,
    string? AssignedCrew);
