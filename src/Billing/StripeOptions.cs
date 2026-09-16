using System.ComponentModel.DataAnnotations;

namespace GridPulse.Billing;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required(AllowEmptyStrings = false, ErrorMessage = "SecretKey must be set (a Stripe test-mode secret key, sk_test_...).")]
    public string SecretKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "WebhookSigningSecret must be set (from `stripe listen --print-secret`).")]
    public string WebhookSigningSecret { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "SuccessUrl must be set.")]
    public string SuccessUrl { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "CancelUrl must be set.")]
    public string CancelUrl { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Currency must be set (a lowercase ISO 4217 code, e.g. \"usd\").")]
    public string Currency { get; set; } = string.Empty;
}
