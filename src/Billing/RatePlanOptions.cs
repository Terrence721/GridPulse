namespace GridPulse.Billing;

public sealed class RatePlanOptions
{
    public const string SectionName = "RatePlan";

    public decimal FlatRatePerKwh { get; set; }

    public IReadOnlyList<RateTier> Tiers { get; set; } = [];

    public decimal PeakRatePerKwh { get; set; }

    public decimal OffPeakRatePerKwh { get; set; }

    public TimeOnly PeakStart { get; set; }

    public TimeOnly PeakEnd { get; set; }
}

public sealed record RateTier(double UpToKwh, decimal PricePerKwh);
