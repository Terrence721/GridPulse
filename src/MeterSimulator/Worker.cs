using Confluent.Kafka;
using Microsoft.Extensions.Options;
using GridPulse.MeterSimulator.Avro;

namespace GridPulse.MeterSimulator;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MeterReadingGenerator _generator;
    private readonly CityBlockMeterIdFactory _meterIdFactory;
    private readonly MeterSimulatorOptions _options;
    private readonly IProducer<string, MeterReadingRaw> _producer;
    private readonly AccountLookupCache _accountLookupCache;

    public Worker(
        ILogger<Worker> logger,
        MeterReadingGenerator generator,
        CityBlockMeterIdFactory meterIdFactory,
        IOptions<MeterSimulatorOptions> options,
        IProducer<string, MeterReadingRaw> producer,
        AccountLookupCache accountLookupCache)
    {
        _logger = logger;
        _generator = generator;
        _meterIdFactory = meterIdFactory;
        _options = options.Value;
        _producer = producer;
        _accountLookupCache = accountLookupCache;
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
                var message = new Message<string, MeterReadingRaw>
                {
                    Key = reading.MeterId,
                    Value = new MeterReadingRaw
                    {
                        MeterId = reading.MeterId,
                        AccountId = _accountLookupCache.GetAccountId(reading.MeterId).ToString(),
                        TimestampUnixMilliseconds = reading.Timestamp.ToUnixTimeMilliseconds(),
                        Kwh = reading.Kwh,
                        ReadingId = reading.ReadingId.ToString()
                    }
                };

                try
                {
                    await _producer.ProduceAsync("meter.readings.raw", message, stoppingToken);

                    _logger.LogInformation(
                        "Sent reading {ReadingId} from {MeterId}: {Kwh} kWh at {Timestamp}",
                        reading.ReadingId, reading.MeterId, reading.Kwh, reading.Timestamp);
                }
                catch (ProduceException<string, MeterReadingRaw> ex)
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
