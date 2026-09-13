using System.ComponentModel.DataAnnotations;

namespace GridPulse.AccountCustomer;

public sealed class AccountSeedOptions
{
    public const string SectionName = "AccountSeed";

    [Required(AllowEmptyStrings = false, ErrorMessage = "StreetName must be set by the field engineer for this deployment.")]
    public string StreetName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "StartingAddress must be set by the field engineer for this deployment.")]
    public int StartingAddress { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BuildingsPerSide must be set by the field engineer for this deployment.")]
    public int BuildingsPerSide { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "NotificationWebhookUrl must be set for this deployment.")]
    public string NotificationWebhookUrl { get; set; } = string.Empty;
}
