using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridPulse.Billing.Tests;

public sealed class InvoicePaymentInitiatorTests
{
    private static BillingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BillingDbContext(options);
    }

    private static InvoicePaymentInitiator CreateInitiator(BillingDbContext dbContext) =>
        new(dbContext, new FakeStripeCheckoutSessionCreator(), NullLogger<InvoicePaymentInitiator>.Instance);

    private static Invoice SeedInvoice(BillingDbContext dbContext, string? existingSessionId = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            AccountId = "ACC-1",
            PeriodStart = DateTimeOffset.UtcNow.AddHours(-1),
            PeriodEnd = DateTimeOffset.UtcNow,
            TotalKwh = 10,
            AmountDue = 1.6m,
            RatePlanType = "Flat",
            GeneratedAt = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            StripeCheckoutSessionId = existingSessionId
        };
        dbContext.Invoices.Add(invoice);
        dbContext.SaveChanges();
        return invoice;
    }

    [Fact]
    public async Task InitiateAsync_NewInvoice_CreatesCheckoutSessionAndPersistsSessionId()
    {
        await using var dbContext = CreateDbContext();
        var invoice = SeedInvoice(dbContext);
        var initiator = CreateInitiator(dbContext);

        await initiator.InitiateAsync(invoice.Id, TestContext.Current.CancellationToken);

        var updated = await dbContext.Invoices.FindAsync([invoice.Id], TestContext.Current.CancellationToken);
        Assert.Equal($"cs_test_{invoice.Id}", updated!.StripeCheckoutSessionId);
    }

    [Fact]
    public async Task InitiateAsync_InvoiceAlreadyHasSession_DoesNotOverwriteIt()
    {
        await using var dbContext = CreateDbContext();
        var invoice = SeedInvoice(dbContext, existingSessionId: "cs_existing");
        var initiator = CreateInitiator(dbContext);

        await initiator.InitiateAsync(invoice.Id, TestContext.Current.CancellationToken);

        var updated = await dbContext.Invoices.FindAsync([invoice.Id], TestContext.Current.CancellationToken);
        Assert.Equal("cs_existing", updated!.StripeCheckoutSessionId);
    }

    [Fact]
    public async Task InitiateAsync_UnknownInvoiceId_DoesNotThrow()
    {
        await using var dbContext = CreateDbContext();
        var initiator = CreateInitiator(dbContext);

        await initiator.InitiateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
    }
}
