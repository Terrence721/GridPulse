namespace GridPulse.MeterSimulator;

public sealed class MeterReadingGenerator
{
    private readonly Random _random = new();

    public MeterReading Generate(string meterId)
    {
        return new MeterReading(
            MeterId: meterId,
            Timestamp: DateTimeOffset.UtcNow,
            Kwh: Math.Round(_random.NextDouble() * 5, 3),
            ReadingId: Guid.NewGuid());
    }
}
