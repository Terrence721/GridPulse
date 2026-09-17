using System.ComponentModel.DataAnnotations;

namespace GridPulse.GridOperations;

public sealed class GridOperationsOptions
{
    public const string SectionName = "GridOperations";

    [Range(1, int.MaxValue, ErrorMessage = "CorrelationAddressWindow must be set to the maximum street-number distance between correlated anomalies.")]
    public int CorrelationAddressWindow { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CorrelationTimeWindowSeconds must be set to the maximum time gap between correlated anomalies.")]
    public int CorrelationTimeWindowSeconds { get; set; }
}
