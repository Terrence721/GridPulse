using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using GridPulse.UsageAggregation;
using GridPulse.UsageAggregation.Avro;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<UsageAggregationDbContext>("gridpulsedb");
builder.Services.AddScoped<ReadingProcessor>();

builder.Services.AddSingleton<ISchemaRegistryClient>(_ =>
{
    var schemaRegistryUrl = builder.Configuration["services:schema-registry:http:0"]
        ?? throw new InvalidOperationException("Schema registry endpoint not configured.");
    return new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl });
});

builder.AddKafkaConsumer<string, MeterReadingRaw>("kafka", settings =>
{
    settings.Config.GroupId = "usage-aggregation";
    settings.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
}, (sp, consumerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    consumerBuilder.SetValueDeserializer(new AvroDeserializer<MeterReadingRaw>(schemaRegistry).AsSyncOverAsync());
});

builder.AddKafkaProducer<string, UsageAggregated>("kafka", (sp, producerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    producerBuilder.SetValueSerializer(new AvroSerializer<UsageAggregated>(schemaRegistry).AsSyncOverAsync());
});
builder.Services.AddSingleton<IUsageAggregatedPublisher, UsageAggregatedPublisher>();

builder.Services.AddHostedService<MeterReadingConsumer>();

var app = builder.Build();
app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UsageAggregationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
