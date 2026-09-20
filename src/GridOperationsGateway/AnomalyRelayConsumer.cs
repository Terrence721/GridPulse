using Confluent.Kafka;
using GridPulse.GridOperationsGateway.Avro;
using GridPulse.GridOperationsGateway.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GridPulse.GridOperationsGateway;

public sealed class AnomalyRelayConsumer(
    IConsumer<string, UsageAnomalyDetected> consumer,
    IHubContext<AnomalyFeedHub> hub,
    ILogger<AnomalyRelayConsumer> logger) : BackgroundService
{
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
                logger.LogWarning(ex, "Failed to consume an anomaly relay message");
                continue;
            }

            var anomaly = result.Message.Value;

            try
            {
                await hub.Clients.All.SendAsync("AnomalyDetected", new
                {
                    meterId = anomaly.MeterId,
                    accountId = anomaly.AccountId,
                    lastSeenAt = DateTimeOffset.FromUnixTimeMilliseconds(anomaly.LastSeenAtUnixMilliseconds),
                    receivedAt = DateTimeOffset.UtcNow
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to relay usage anomaly for meter {MeterId}", anomaly.MeterId);
            }
        }
    }
}
