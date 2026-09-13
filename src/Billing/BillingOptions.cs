using System.ComponentModel.DataAnnotations;

namespace GridPulse.Billing;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    [Required(AllowEmptyStrings = false, ErrorMessage = "DefaultRatePlanType must be set (\"Flat\", \"Tiered\", or \"TimeOfUse\").")]
    public string DefaultRatePlanType { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "PaymentTermsDays must be set to a positive number of days.")]
    public int PaymentTermsDays { get; set; }
}
