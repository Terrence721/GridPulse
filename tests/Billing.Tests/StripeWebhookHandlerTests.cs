using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;

namespace GridPulse.Billing.Tests;

public sealed class StripeWebhookHandlerTests
{
    private const string WebhookSecret = "whsec_test_secret";

    // Stripe.net's own ApiVersion.Current holds this value but the class is internal to the
    // SDK, so it can't be referenced here. EventUtility.ConstructEvent throws on a mismatch
    // between this and the pinned Stripe.net version's compiled-in version - keep this
    // literal in sync with whatever version is pinned in GridPulse.Billing.csproj.
    private const string StripeApiVersion = "2026-08-26.dahlia";

    private static BillingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BillingDbContext(options);
    }

    private static StripeWebhookHandler CreateHandler(BillingDbContext dbContext) =>
        new(
            dbContext,
            Options.Create(new StripeOptions
            {
                SecretKey = "sk_test_unused",
                WebhookSigningSecret = WebhookSecret,
                SuccessUrl = "http://localhost/checkout/success",
                CancelUrl = "http://localhost/checkout/cancelled",
                Currency = "usd"
            }),
            NullLogger<StripeWebhookHandler>.Instance);

    private static async Task<Invoice> SeedInvoiceAsync(BillingDbContext dbContext, string status = "Open")
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
            Status = status
        };
        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();
        return invoice;
    }

    private static string CheckoutSessionCompletedPayload(Guid invoiceId) => $$"""
        {
          "id": "evt_test_1",
          "object": "event",
          "api_version": "{{StripeApiVersion}}",
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "cs_test_1",
              "object": "checkout.session",
              "payment_intent": "pi_test_1",
              "metadata": { "invoiceId": "{{invoiceId}}" }
            }
          }
        }
        """;

    private static string PaymentFailedPayload(Guid invoiceId) => $$"""
        {
          "id": "evt_test_2",
          "object": "event",
          "api_version": "{{StripeApiVersion}}",
          "type": "payment_intent.payment_failed",
          "data": {
            "object": {
              "id": "pi_test_2",
              "object": "payment_intent",
              "metadata": { "invoiceId": "{{invoiceId}}" }
            }
          }
        }
        """;

    [Fact]
    public async Task HandleAsync_ValidCheckoutSessionCompleted_MarksInvoicePaidWithPaymentIntentId()
    {
        await using var dbContext = CreateDbContext();
        var invoice = await SeedInvoiceAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var json = CheckoutSessionCompletedPayload(invoice.Id);
        var signature = EventUtility.GenerateSignatureHeader(json, WebhookSecret);

        var result = await handler.HandleAsync(json, signature, CancellationToken.None);

        Assert.True(result);
        var updated = await dbContext.Invoices.FindAsync(invoice.Id);
        Assert.Equal("Paid", updated!.Status);
        Assert.Equal("pi_test_1", updated.StripePaymentIntentId);
        Assert.NotNull(updated.PaidAt);
    }

    [Fact]
    public async Task HandleAsync_PaymentFailed_MarksInvoicePaymentFailed()
    {
        await using var dbContext = CreateDbContext();
        var invoice = await SeedInvoiceAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var json = PaymentFailedPayload(invoice.Id);
        var signature = EventUtility.GenerateSignatureHeader(json, WebhookSecret);

        var result = await handler.HandleAsync(json, signature, CancellationToken.None);

        Assert.True(result);
        var updated = await dbContext.Invoices.FindAsync(invoice.Id);
        Assert.Equal("PaymentFailed", updated!.Status);
    }

    [Fact]
    public async Task HandleAsync_InvalidSignature_ReturnsFalseAndDoesNotModifyInvoice()
    {
        await using var dbContext = CreateDbContext();
        var invoice = await SeedInvoiceAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var json = CheckoutSessionCompletedPayload(invoice.Id);

        var result = await handler.HandleAsync(json, "t=1,v1=deadbeef", CancellationToken.None);

        Assert.False(result);
        var updated = await dbContext.Invoices.FindAsync(invoice.Id);
        Assert.Equal("Open", updated!.Status);
    }

    [Fact]
    public async Task HandleAsync_AlreadyPaidInvoice_IgnoresLateArrivingPaymentFailedEvent()
    {
        await using var dbContext = CreateDbContext();
        var invoice = await SeedInvoiceAsync(dbContext, status: "Paid");
        var handler = CreateHandler(dbContext);
        var json = PaymentFailedPayload(invoice.Id);
        var signature = EventUtility.GenerateSignatureHeader(json, WebhookSecret);

        var result = await handler.HandleAsync(json, signature, CancellationToken.None);

        Assert.True(result);
        var updated = await dbContext.Invoices.FindAsync(invoice.Id);
        Assert.Equal("Paid", updated!.Status);
    }

    [Fact]
    public async Task HandleAsync_UnknownInvoiceId_ReturnsTrueWithoutThrowing()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);
        var json = CheckoutSessionCompletedPayload(Guid.NewGuid());
        var signature = EventUtility.GenerateSignatureHeader(json, WebhookSecret);

        var result = await handler.HandleAsync(json, signature, CancellationToken.None);

        Assert.True(result);
    }
}
