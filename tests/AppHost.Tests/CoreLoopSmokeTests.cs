using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using GridPulse.AppHost.Tests.Avro;
using Aspire.Hosting.Testing;

namespace GridPulse.AppHost.Tests;

public sealed class CoreLoopSmokeTests
{
    [Fact]
    public async Task CoreLoop_ReadingFlowsThroughKafkaToBilling()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.GridPulse_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("usage-aggregation", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("billing", cancellationToken);

        var kafkaBootstrapServers = await app.GetConnectionStringAsync("kafka", cancellationToken)
            ?? throw new InvalidOperationException("Kafka connection string not available.");
        var schemaRegistryUrl = app.GetEndpoint("schema-registry", "http").ToString();

        using var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl });

        var meterId = $"SMOKE-TEST-METER-{Guid.NewGuid()}";

        using var producer = new ProducerBuilder<string, MeterReadingRaw>(new ProducerConfig { BootstrapServers = kafkaBootstrapServers })
            .SetValueSerializer(new AvroSerializer<MeterReadingRaw>(schemaRegistry).AsSyncOverAsync())
            .Build();

        await producer.ProduceAsync("meter.readings.raw", new Message<string, MeterReadingRaw>
        {
            Key = meterId,
            Value = new MeterReadingRaw
            {
                MeterId = meterId,
                TimestampUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Kwh = 10.0,
                ReadingId = Guid.NewGuid().ToString()
            }
        }, cancellationToken);
        producer.Flush(cancellationToken);

        using var consumer = new ConsumerBuilder<string, BillingInvoiceGenerated>(new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = $"smoke-test-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        })
            .SetValueDeserializer(new AvroDeserializer<BillingInvoiceGenerated>(schemaRegistry).AsSyncOverAsync())
            .Build();
        consumer.Subscribe("billing.invoice.generated");

        BillingInvoiceGenerated? invoice = null;
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline && invoice is null)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            if (result is not null && result.Message.Value.MeterId == meterId)
            {
                invoice = result.Message.Value;
            }
        }

        Assert.NotNull(invoice);
        Assert.Equal(meterId, invoice.MeterId);
        Assert.True(invoice.AmountDue > 0);
    }
}
