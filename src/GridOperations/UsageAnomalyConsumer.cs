using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using GridPulse.GridOperations.Avro;

namespace GridPulse.GridOperations;

public sealed class UsageAnomalyConsumer(
    IConsumer<string, UsageAnomalyDetected> consumer,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<UsageAnomalyConsumer> logger) : BackgroundService
{
    // AccountCustomer's minimal API serializes responses with the ASP.NET Core Web
    // defaults (camelCase), but GetFromJsonAsync without explicit options deserializes
    // case-sensitively against this PascalCase record - StreetName/StreetNumber would
    // silently bind to their defaults (null/0) with no exception.
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe("usage.anomaly.detected");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, UsageAnomalyDetected> result;

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
                logger.LogWarning(ex, "Failed to consume a usage anomaly message");
                continue;
            }

            var anomaly = result.Message.Value;

            try
            {
                var httpClient = httpClientFactory.CreateClient("account-customer");
                MeterLookupResponse? meter;

                try
                {
                    meter = await httpClient.GetFromJsonAsync<MeterLookupResponse>(
                        $"/meters/{Uri.EscapeDataString(anomaly.MeterId)}", ResponseOptions, stoppingToken);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    meter = null;
                }

                if (meter is null)
                {
                    logger.LogWarning("Usage anomaly for unknown meter {MeterId} - skipping", anomaly.MeterId);
                    continue;
                }

                var detectedAnomaly = new DetectedAnomaly(
                    anomaly.MeterId,
                    anomaly.AccountId,
                    meter.StreetName,
                    meter.StreetNumber,
                    DateTimeOffset.FromUnixTimeMilliseconds(anomaly.LastSeenAtUnixMilliseconds));

                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<WorkOrderCorrelationProcessor>();
                await processor.ProcessAsync(detectedAnomaly, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process usage anomaly for meter {MeterId}", anomaly.MeterId);
            }
        }
    }

    private sealed record MeterLookupResponse(string StreetName, int StreetNumber);
}
