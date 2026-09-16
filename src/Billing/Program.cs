using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using GridPulse.Billing;
using GridPulse.Billing.Avro;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<BillingDbContext>("gridpulsedb");
builder.Services.Configure<RatePlanOptions>(
    builder.Configuration.GetSection(RatePlanOptions.SectionName));
builder.Services.Configure<BillingOptions>(
    builder.Configuration.GetSection(BillingOptions.SectionName));
builder.Services.AddOptions<BillingOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddOptions<StripeOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddScoped<InvoiceGenerator>();

builder.Services.AddSingleton<ISchemaRegistryClient>(_ =>
{
    var schemaRegistryUrl = builder.Configuration["services:schema-registry:http:0"]
        ?? throw new InvalidOperationException("Schema registry endpoint not configured.");
    return new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl });
});

builder.AddKafkaConsumer<string, UsageAggregated>("kafka", settings =>
{
    settings.Config.GroupId = "billing";
    settings.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
}, (sp, consumerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    consumerBuilder.SetValueDeserializer(new AvroDeserializer<UsageAggregated>(schemaRegistry).AsSyncOverAsync());
});

builder.AddKafkaProducer<string, BillingInvoiceGenerated>("kafka", (sp, producerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    producerBuilder.SetValueSerializer(new AvroSerializer<BillingInvoiceGenerated>(schemaRegistry).AsSyncOverAsync());
});
builder.Services.AddSingleton<IBillingInvoiceGeneratedPublisher, BillingInvoiceGeneratedPublisher>();

builder.Services.AddHostedService<UsageAggregatedConsumer>();

// A new, independent consumer of the existing billing.invoice.generated topic - Latest, not
// Earliest, because this topic has real history since Phase 2 and Earliest would fire a real
// Stripe API call for every historical invoice on first startup (unlike
// UsageAggregatedConsumer's Earliest, which only ever re-runs free, idempotent Postgres
// upserts against internal data).
builder.AddKafkaConsumer<string, BillingInvoiceGenerated>("kafka", settings =>
{
    settings.Config.GroupId = "billing-payments";
    settings.Config.AutoOffsetReset = AutoOffsetReset.Latest;
}, (sp, consumerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    consumerBuilder.SetValueDeserializer(new AvroDeserializer<BillingInvoiceGenerated>(schemaRegistry).AsSyncOverAsync());
});
builder.Services.AddHostedService<InvoicePaymentConsumer>();

builder.Services.AddHttpClient("stripe");
builder.Services.AddSingleton<IStripeClient>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("stripe");
    var stripeOptions = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
    // maxNetworkRetries: 0 - ServiceDefaults' AddStandardResilienceHandler() already wraps
    // this named HttpClient with its own retry policy; letting Stripe.net retry too would
    // stack two retry layers on the same failure.
    var stripeHttpClient = new SystemNetHttpClient(httpClient, maxNetworkRetries: 0);
    return new StripeClient(apiKey: stripeOptions.SecretKey, httpClient: stripeHttpClient);
});
builder.Services.AddScoped<IStripeCheckoutSessionCreator, StripeCheckoutSessionCreator>();
builder.Services.AddScoped<InvoicePaymentInitiator>();
builder.Services.AddScoped<StripeWebhookHandler>();

var app = builder.Build();
app.MapDefaultEndpoints();

try
{
    // Trigger validation here, before app.Run() starts the hosting pipeline — resolving it
    // inside app.Run() means the Generic Host's own StartAsync() logs the exception (with a
    // full stack trace) before this catch ever runs. BillingOptions was bound with
    // ValidateOnStart() but never actually resolved this way despite that - retrofitted here
    // alongside StripeOptions rather than left as a second, inconsistent validation path.
    _ = app.Services.GetRequiredService<IOptions<BillingOptions>>().Value;
    _ = app.Services.GetRequiredService<IOptions<StripeOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    dbContext.Database.Migrate();
}

app.MapPost("/webhooks/stripe", async (HttpRequest request, StripeWebhookHandler handler, CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync(cancellationToken);
    var signatureHeader = request.Headers["Stripe-Signature"].ToString();

    var verified = await handler.HandleAsync(json, signatureHeader, cancellationToken);
    return verified ? Results.Ok() : Results.BadRequest();
});

app.MapGet("/checkout/success", () => Results.Content("Payment received - thank you.", "text/plain"));
app.MapGet("/checkout/cancelled", () => Results.Content("Payment cancelled.", "text/plain"));

app.Run();
return 0;
