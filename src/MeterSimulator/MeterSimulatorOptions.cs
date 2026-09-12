using System.ComponentModel.DataAnnotations;

namespace GridPulse.MeterSimulator;

public sealed class MeterSimulatorOptions
{
    public const string SectionName = "MeterSimulator";

    public int IntervalSeconds { get; set; } = 5;

    [Required(AllowEmptyStrings = false, ErrorMessage = "StreetName must be set by the field engineer for this deployment.")]
    public string StreetName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "StartingAddress must be set by the field engineer for this deployment.")]
    public int StartingAddress { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BuildingsPerSide must be set by the field engineer for this deployment.")]
    public int BuildingsPerSide { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "City must be set by the field engineer for this deployment.")]
    public string City { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "ZipCode must be set by the field engineer for this deployment.")]
    public string ZipCode { get; set; } = string.Empty;
}
