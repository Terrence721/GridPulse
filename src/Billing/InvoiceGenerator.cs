using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace GridPulse.Billing;

public sealed class InvoiceGenerator(
    IHttpClientFactory httpClientFactory,
    BillingDbContext dbContext,
    IOptions<RatePlanOptions> ratePlanOptions)
{
    public async Task<Invoice> GenerateAsync(string meterId, DateTimeOffset periodStart, DateTimeOffset periodEnd, string ratePlanType, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("usage-aggregation");
        var hourlyUsage = await httpClient.GetFromJsonAsync<List<HourlyUsageSnapshot>>(
            $"/usage/{meterId}?periodStart={Uri.EscapeDataString(periodStart.ToString("O"))}&periodEnd={Uri.EscapeDataString(periodEnd.ToString("O"))}",
            cancellationToken) ?? [];

        var ratePlan = CreateRatePlan(ratePlanType, ratePlanOptions.Value);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            MeterId = meterId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalKwh = hourlyUsage.Sum(u => u.TotalKwh),
            AmountDue = ratePlan.CalculateCost(hourlyUsage),
            RatePlanType = ratePlanType,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    private static IRatePlan CreateRatePlan(string ratePlanType, RatePlanOptions options) => ratePlanType switch
    {
        "Flat" => new FlatRateRatePlan(options.FlatRatePerKwh),
        "Tiered" => new TieredRatePlan(options.Tiers),
        "TimeOfUse" => new TimeOfUseRatePlan(options.PeakRatePerKwh, options.OffPeakRatePerKwh, options.PeakStart, options.PeakEnd),
        _ => throw new ArgumentOutOfRangeException(nameof(ratePlanType), ratePlanType, "Unknown rate plan type. Expected \"Flat\", \"Tiered\", or \"TimeOfUse\".")
    };
}
