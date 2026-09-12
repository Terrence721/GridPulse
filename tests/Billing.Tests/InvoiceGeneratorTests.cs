using System.Net;
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

    private static InvoiceGenerator CreateGenerator(BillingDbContext dbContext, string usageResponseJson)
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, usageResponseJson);
        var httpClientFactory = new FakeHttpClientFactory(handler);
        return new InvoiceGenerator(httpClientFactory, dbContext, Options.Create(RatePlanOptions()));
    }

    [Fact]
    public async Task GenerateAsync_FlatPlan_PersistsInvoiceWithCorrectAmountDue()
    {
        const string usageJson = """[{"MeterId":"MTR-100-Elm St","PeriodStart":"2026-09-12T17:00:00+00:00","PeriodEnd":"2026-09-12T18:00:00+00:00","TotalKwh":10.0}]""";
        await using var dbContext = CreateDbContext();
        var generator = CreateGenerator(dbContext, usageJson);

        var invoice = await generator.GenerateAsync(
            "MTR-100-Elm St",
            new DateTimeOffset(2026, 9, 12, 17, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.Zero),
            "Flat",
            CancellationToken.None);

        Assert.Equal(1.6m, invoice.AmountDue);
        Assert.Equal(10.0, invoice.TotalKwh);
        Assert.Single(dbContext.Invoices);
    }

    [Fact]
    public async Task GenerateAsync_UnknownRatePlanType_ThrowsArgumentOutOfRangeException()
    {
        await using var dbContext = CreateDbContext();
        var generator = CreateGenerator(dbContext, "[]");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => generator.GenerateAsync(
            "MTR-100-Elm St",
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow,
            "Unknown",
            CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_NoUsageReturned_PersistsZeroAmountInvoice()
    {
        await using var dbContext = CreateDbContext();
        var generator = CreateGenerator(dbContext, "[]");

        var invoice = await generator.GenerateAsync(
            "MTR-100-Elm St",
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow,
            "Flat",
            CancellationToken.None);

        Assert.Equal(0m, invoice.AmountDue);
        Assert.Equal(0.0, invoice.TotalKwh);
    }
}
