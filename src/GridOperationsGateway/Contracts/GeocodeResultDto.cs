namespace GridPulse.GridOperationsGateway.Contracts;

public sealed record GeocodeResultDto
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
