using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using GridPulse.Billing;
using GridPulse.Billing.Avro;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();
app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
