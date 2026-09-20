using System.ComponentModel.DataAnnotations;

namespace GridPulse.Identity;

public sealed class SmokeTestClientOptions
{
    public const string SectionName = "SmokeTestClient";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Secret must be set (the grid-ops-console-smoke-test client's secret).")]
    public string Secret { get; set; } = string.Empty;
}
