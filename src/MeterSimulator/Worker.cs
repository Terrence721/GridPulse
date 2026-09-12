using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace GridPulse.MeterSimulator;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MeterReadingGenerator _generator;
    private readonly CityBlockMeterIdFactory _meterIdFactory;
    private readonly MeterSimulatorOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public Worker(
        ILogger<Worker> logger,
        MeterReadingGenerator generator,
        CityBlockMeterIdFactory meterIdFactory,
        IOptions<MeterSimulatorOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _generator = generator;
        _meterIdFactory = meterIdFactory;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var meterIds = _meterIdFactory.Create(_options);
        var interval = TimeSpan.FromSeconds(_options.IntervalSeconds);
        var httpClient = _httpClientFactory.CreateClient("usage-aggregation");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var meterId in meterIds)
            {
                var reading = _generator.Generate(meterId);

                try
                {
                    var response = await httpClient.PostAsJsonAsync("/readings", reading, stoppingToken);
                    response.EnsureSuccessStatusCode();

                    _logger.LogInformation(
                        "Sent reading {ReadingId} from {MeterId}: {Kwh} kWh at {Timestamp}",
                        reading.ReadingId, reading.MeterId, reading.Kwh, reading.Timestamp);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to send reading {ReadingId} from {MeterId}",
                        reading.ReadingId, reading.MeterId);
                }
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
