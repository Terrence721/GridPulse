using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using GridPulse.UsageAggregation;
using GridPulse.UsageAggregation.Avro;
using GridPulse.UsageAggregation.Espi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<UsageAggregationDbContext>("gridpulsedb");
builder.Services.AddScoped<ReadingProcessor>();
builder.Services.AddScoped<UsageAnomalyProcessor>();

builder.AddValidatedOptions<UsageAnomalyOptions>(UsageAnomalyOptions.SectionName);

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

builder.AddKafkaProducer<string, UsageAnomalyDetected>("kafka", (sp, producerBuilder) =>
{
    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
    producerBuilder.SetValueSerializer(new AvroSerializer<UsageAnomalyDetected>(schemaRegistry).AsSyncOverAsync());
});
builder.Services.AddSingleton<IUsageAnomalyPublisher, UsageAnomalyPublisher>();

builder.Services.AddHostedService<MeterReadingConsumer>();
builder.Services.AddHostedService<UsageAnomalyDetector>();

var app = builder.Build();
app.MapDefaultEndpoints();

if (!app.Services.TryValidateStartupOptions<UsageAnomalyOptions>())
{
    return 1;
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UsageAggregationDbContext>();
    dbContext.Database.Migrate();
}

app.MapGet(EspiConstants.RoutePrefix + "/UsagePoint", async (UsageAggregationDbContext db, HttpRequest request) =>
{
    var accountIds = await db.HourlyUsages.Where(u => u.AccountId != "").Select(u => u.AccountId).Distinct().ToListAsync();
    var baseUrl = $"{request.Scheme}://{request.Host}";
    var xml = EspiFeedBuilder.ToXmlString(EspiFeedBuilder.BuildUsagePointCollectionFeed(baseUrl, accountIds, DateTimeOffset.UtcNow));
    return Results.Text(xml, EspiConstants.AtomContentType);
});

app.MapGet(EspiConstants.RoutePrefix + "/UsagePoint/{accountId}", async (string accountId, UsageAggregationDbContext db, HttpRequest request) =>
{
    if (!await db.HourlyUsages.AnyAsync(u => u.AccountId == accountId)) { return Results.NotFound(); }
    var baseUrl = $"{request.Scheme}://{request.Host}";
    var xml = EspiFeedBuilder.ToXmlString(EspiFeedBuilder.BuildUsagePointFeed(baseUrl, accountId, DateTimeOffset.UtcNow));
    return Results.Text(xml, EspiConstants.AtomContentType);
});

app.MapGet(EspiConstants.RoutePrefix + "/UsagePoint/{accountId}/MeterReading", async (string accountId, UsageAggregationDbContext db, HttpRequest request) =>
{
    if (!await db.HourlyUsages.AnyAsync(u => u.AccountId == accountId)) { return Results.NotFound(); }
    var baseUrl = $"{request.Scheme}://{request.Host}";
    var xml = EspiFeedBuilder.ToXmlString(EspiFeedBuilder.BuildMeterReadingFeed(baseUrl, accountId, DateTimeOffset.UtcNow));
    return Results.Text(xml, EspiConstants.AtomContentType);
});

app.MapGet(EspiConstants.RoutePrefix + "/UsagePoint/{accountId}/MeterReading/{meterReadingId}/ReadingType", async (string accountId, string meterReadingId, UsageAggregationDbContext db, HttpRequest request) =>
{
    if (meterReadingId != EspiConstants.MeterReadingId) { return Results.NotFound(); }
    if (!await db.HourlyUsages.AnyAsync(u => u.AccountId == accountId)) { return Results.NotFound(); }
    var baseUrl = $"{request.Scheme}://{request.Host}";
    var xml = EspiFeedBuilder.ToXmlString(EspiFeedBuilder.BuildReadingTypeFeed(baseUrl, accountId, DateTimeOffset.UtcNow));
    return Results.Text(xml, EspiConstants.AtomContentType);
});

app.MapGet(EspiConstants.RoutePrefix + "/UsagePoint/{accountId}/MeterReading/{meterReadingId}/IntervalBlock", async (
    string accountId,
    string meterReadingId,
    [FromQuery(Name = "published-min")] long? publishedMin,
    [FromQuery(Name = "published-max")] long? publishedMax,
    UsageAggregationDbContext db,
    HttpRequest request) =>
{
    if (meterReadingId != EspiConstants.MeterReadingId) { return Results.NotFound(); }
    if (!await db.HourlyUsages.AnyAsync(u => u.AccountId == accountId)) { return Results.NotFound(); }

    var query = db.HourlyUsages.Where(u => u.AccountId == accountId);
    if (publishedMin is not null)
    {
        var min = DateTimeOffset.FromUnixTimeSeconds(publishedMin.Value);
        query = query.Where(u => u.PeriodStart >= min);
    }
    if (publishedMax is not null)
    {
        var max = DateTimeOffset.FromUnixTimeSeconds(publishedMax.Value);
        query = query.Where(u => u.PeriodStart <= max);
    }

    var readings = await query.OrderBy(u => u.PeriodStart).ToListAsync();
    var baseUrl = $"{request.Scheme}://{request.Host}";
    var xml = EspiFeedBuilder.ToXmlString(EspiFeedBuilder.BuildIntervalBlockFeed(baseUrl, accountId, readings, DateTimeOffset.UtcNow));
    return Results.Text(xml, EspiConstants.AtomContentType);
});

app.Run();
return 0;
