using System.ComponentModel.DataAnnotations;

namespace GridPulse.UsageAggregation;

public sealed class UsageAnomalyOptions
{
    public const string SectionName = "UsageAnomaly";

    [Range(1, int.MaxValue, ErrorMessage = "ExpectedReadingIntervalSeconds must be set to the meter's normal reading cadence.")]
    public int ExpectedReadingIntervalSeconds { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MissedIntervalMultiplier must be set to a positive number of missed intervals before an anomaly is declared.")]
    public int MissedIntervalMultiplier { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SweepIntervalSeconds must be set to how often the detector checks for stale meters.")]
    public int SweepIntervalSeconds { get; set; }
}
