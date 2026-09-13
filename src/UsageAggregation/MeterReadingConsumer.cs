using Confluent.Kafka;
using GridPulse.UsageAggregation.Avro;

namespace GridPulse.UsageAggregation;

public sealed class MeterReadingConsumer(
    IConsumer<string, MeterReadingRaw> consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<MeterReadingConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe("meter.readings.raw");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, MeterReadingRaw> result;

            try
            {
                result = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogWarning(ex, "Failed to consume a meter reading message");
                continue;
            }

            var raw = result.Message.Value;
            var request = new MeterReadingRequest(
                raw.MeterId,
                raw.AccountId,
                DateTimeOffset.FromUnixTimeMilliseconds(raw.TimestampUnixMilliseconds),
                raw.Kwh,
                Guid.Parse(raw.ReadingId));

            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ReadingProcessor>();
            await processor.ProcessAsync(request, stoppingToken);
        }
    }
}
