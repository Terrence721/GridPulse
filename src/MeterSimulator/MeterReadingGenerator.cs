namespace GridPulse.MeterSimulator;

public sealed class MeterReadingGenerator(HouseholdPowerDataset dataset)
{
    private readonly Dictionary<string, int> _cursorsByMeterId = new();

    public MeterReading Generate(string meterId, int intervalSeconds)
    {
        if (!_cursorsByMeterId.TryGetValue(meterId, out var cursor))
        {
            cursor = Math.Abs(meterId.GetHashCode()) % dataset.Count;
        }

        var kw = dataset.GlobalActivePowerKwAt(cursor);
        var kwh = Math.Round(kw * intervalSeconds / 3600.0, 6);
        _cursorsByMeterId[meterId] = (cursor + 1) % dataset.Count;

        return new MeterReading(
            MeterId: meterId,
            Timestamp: DateTimeOffset.UtcNow,
            Kwh: kwh,
            ReadingId: Guid.NewGuid());
    }
}
