using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridPulse.Billing.Tests;

public sealed class InvoiceGeneratorTests
{
    private static BillingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BillingDbContext(options);
    }

    private static RatePlanOptions RatePlanOptions() => new()
    {
        FlatRatePerKwh = 0.16m,
        Tiers = [new RateTier(500, 0.14m), new RateTier(1000, 0.17m), new RateTier(100000, 0.21m)],
        PeakRatePerKwh = 0.22m,
        OffPeakRatePerKwh = 0.10m,
        PeakStart = new TimeOnly(14, 0),
        PeakEnd = new TimeOnly(19, 0)
    };

    private static InvoiceGenerator CreateGenerator(BillingDbContext dbContext, string defaultRatePlanType = "Flat") =>
        new(
            dbContext,
            Options.Create(RatePlanOptions()),
            Options.Create(new BillingOptions { DefaultRatePlanType = defaultRatePlanType, PaymentTermsDays = 30 }),
            new FakeBillingInvoiceGeneratedPublisher());

    [Fact]
    public async Task GenerateAsync_FlatPlan_PersistsInvoiceWithCorrectAmountDue()
    {
        await using var dbContext = CreateDbContext();
        var generator = CreateGenerator(dbContext);

        var invoice = await generator.GenerateAsync(
            "MTR-100-Elm St",
            new DateTimeOffset(2026, 9, 12, 17, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.Zero),
            10.0,
            CancellationToken.None);

        Assert.Equal(1.6m, invoice.AmountDue);
        Assert.Equal(10.0, invoice.TotalKwh);
        Assert.Single(dbContext.Invoices);
    }

    [Fact]
    public async Task GenerateAsync_UnknownRatePlanType_ThrowsArgumentOutOfRangeException()
    {
        await using var dbContext = CreateDbContext();
        var generator = CreateGenerator(dbContext, defaultRatePlanType: "Unknown");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => generator.GenerateAsync(
            "MTR-100-Elm St",
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow,
            10.0,
            CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_NoUsage_PersistsZeroAmountInvoice()
    {
        await using var dbContext = CreateDbContext();
        var generator = CreateGenerator(dbContext);

        var invoice = await generator.GenerateAsync(
            "MTR-100-Elm St",
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow,
            0.0,
            CancellationToken.None);

        Assert.Equal(0m, invoice.AmountDue);
        Assert.Equal(0.0, invoice.TotalKwh);
    }

    [Fact]
    public async Task GenerateAsync_CalledTwiceForSamePeriod_UpsertsInsteadOfDuplicating()
    {
        await using var dbContext = CreateDbContext();
        var generator = CreateGenerator(dbContext);
        var periodStart = new DateTimeOffset(2026, 9, 12, 17, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddHours(1);

        await generator.GenerateAsync("MTR-100-Elm St", periodStart, periodEnd, 10.0, CancellationToken.None);
        var invoice = await generator.GenerateAsync("MTR-100-Elm St", periodStart, periodEnd, 25.0, CancellationToken.None);

        Assert.Single(dbContext.Invoices);
        Assert.Equal(25.0, invoice.TotalKwh);
        Assert.Equal(4.0m, invoice.AmountDue);
    }
}
