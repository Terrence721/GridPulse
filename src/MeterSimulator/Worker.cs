using Microsoft.Extensions.Options;

namespace GridPulse.MeterSimulator;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MeterReadingGenerator _generator;
    private readonly CityBlockMeterIdFactory _meterIdFactory;
    private readonly MeterSimulatorOptions _options;

    public Worker(
        ILogger<Worker> logger,
        MeterReadingGenerator generator,
        CityBlockMeterIdFactory meterIdFactory,
        IOptions<MeterSimulatorOptions> options)
    {
        _logger = logger;
        _generator = generator;
        _meterIdFactory = meterIdFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var meterIds = _meterIdFactory.Create(_options);
        var interval = TimeSpan.FromSeconds(_options.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var meterId in meterIds)
            {
                var reading = _generator.Generate(meterId);
                _logger.LogInformation(
                    "Reading {ReadingId} from {MeterId}: {Kwh} kWh at {Timestamp}",
                    reading.ReadingId, reading.MeterId, reading.Kwh, reading.Timestamp);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
