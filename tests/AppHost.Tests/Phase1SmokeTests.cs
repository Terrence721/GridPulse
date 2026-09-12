using System.Net.Http.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace GridPulse.AppHost.Tests;

public sealed class Phase1SmokeTests
{
    [Fact]
    public async Task CoreLoop_ReadingFlowsThroughUsageAggregationToBilling()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.GridPulse_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("usage-aggregation", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("billing", cancellationToken);
        await app.ResourceNotifications.WaitForResourceAsync("meter-simulator", KnownResourceStates.Running, cancellationToken);

        var usageAggregationClient = app.CreateHttpClient("usage-aggregation");
        var billingClient = app.CreateHttpClient("billing");

        const string meterId = "SMOKE-TEST-METER";
        var periodStart = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var readingResponse = await usageAggregationClient.PostAsJsonAsync("/readings", new
        {
            meterId,
            timestamp = periodStart.AddMinutes(15),
            kwh = 10.0,
            readingId = Guid.NewGuid()
        }, cancellationToken);
        readingResponse.EnsureSuccessStatusCode();

        var invoiceResponse = await billingClient.PostAsJsonAsync("/invoices", new
        {
            meterId,
            periodStart,
            periodEnd = periodStart.AddHours(1),
            ratePlanType = "Flat"
        }, cancellationToken);
        invoiceResponse.EnsureSuccessStatusCode();

        var invoice = await invoiceResponse.Content.ReadFromJsonAsync<InvoiceResult>(cancellationToken);

        Assert.NotNull(invoice);
        Assert.Equal(10.0, invoice.TotalKwh);
        Assert.True(invoice.AmountDue > 0);
    }

    private sealed record InvoiceResult(Guid Id, string MeterId, double TotalKwh, decimal AmountDue, string RatePlanType);
}
