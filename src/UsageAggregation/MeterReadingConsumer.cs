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

            if (string.IsNullOrEmpty(raw.AccountId))
            {
                logger.LogWarning("Skipping meter reading {ReadingId} from {MeterId} with no AccountId", raw.ReadingId, raw.MeterId);
                continue;
            }

            if (!Guid.TryParse(raw.ReadingId, out var readingId))
            {
                logger.LogWarning("Skipping meter reading from {MeterId} with invalid ReadingId '{ReadingId}'", raw.MeterId, raw.ReadingId);
                continue;
            }

            var request = new MeterReadingRequest(
                raw.MeterId,
                raw.AccountId,
                DateTimeOffset.FromUnixTimeMilliseconds(raw.TimestampUnixMilliseconds),
                raw.Kwh,
                readingId);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ReadingProcessor>();
                await processor.ProcessAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                // A failure here (transient DB error, etc.) must never crash this
                // BackgroundService - .NET's default BackgroundServiceExceptionBehavior is
                // StopHost, which would permanently stop meter-reading processing for the
                // rest of the process's lifetime over one bad/transient failure.
                logger.LogWarning(ex, "Failed to process meter reading {ReadingId} from {MeterId}", raw.ReadingId, raw.MeterId);
            }
        }
    }
}
