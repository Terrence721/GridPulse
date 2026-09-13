using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridPulse.Billing;

public sealed class InvoiceGenerator(
    BillingDbContext dbContext,
    IOptions<RatePlanOptions> ratePlanOptions,
    IOptions<BillingOptions> billingOptions,
    IBillingInvoiceGeneratedPublisher publisher)
{
    public async Task<Invoice> GenerateAsync(string meterId, DateTimeOffset periodStart, DateTimeOffset periodEnd, double totalKwh, CancellationToken cancellationToken)
    {
        var ratePlanType = billingOptions.Value.DefaultRatePlanType;
        var ratePlan = CreateRatePlan(ratePlanType, ratePlanOptions.Value);
        var hourlyUsage = new List<HourlyUsageSnapshot> { new(meterId, periodStart, periodEnd, totalKwh) };
        var amountDue = ratePlan.CalculateCost(hourlyUsage);

        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(
            i => i.MeterId == meterId && i.PeriodStart == periodStart && i.PeriodEnd == periodEnd && i.RatePlanType == ratePlanType,
            cancellationToken);

        if (invoice is null)
        {
            invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                MeterId = meterId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                RatePlanType = ratePlanType
            };
            dbContext.Invoices.Add(invoice);
        }

        invoice.TotalKwh = totalKwh;
        invoice.AmountDue = amountDue;
        invoice.GeneratedAt = DateTimeOffset.UtcNow;
        invoice.DueDate = invoice.GeneratedAt.AddDays(billingOptions.Value.PaymentTermsDays);

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.PublishAsync(invoice, cancellationToken);

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
