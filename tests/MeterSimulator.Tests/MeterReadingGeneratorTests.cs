namespace GridPulse.MeterSimulator.Tests;

public sealed class MeterReadingGeneratorTests
{
    [Fact]
    public void Generate_UsesProvidedMeterId()
    {
        var generator = new MeterReadingGenerator(new HouseholdPowerDataset());

        var reading = generator.Generate("MTR-100-Elm St", intervalSeconds: 5);

        Assert.Equal("MTR-100-Elm St", reading.MeterId);
    }

    [Fact]
    public void Generate_ProducesNonNegativeKwh()
    {
        var generator = new MeterReadingGenerator(new HouseholdPowerDataset());

        var reading = generator.Generate("MTR-100-Elm St", intervalSeconds: 5);

        Assert.True(reading.Kwh >= 0);
    }

    [Fact]
    public void Generate_ConvertsRealDatasetKwToKwhForTheGivenInterval()
    {
        var dataset = new HouseholdPowerDataset();
        var generator = new MeterReadingGenerator(dataset);
        const string meterId = "MTR-100-Elm St";
        var expectedCursor = Math.Abs(meterId.GetHashCode()) % dataset.Count;
        var expectedKw = dataset.GlobalActivePowerKwAt(expectedCursor);
        var expectedKwh = Math.Round(expectedKw * 5 / 3600.0, 6);

        var reading = generator.Generate(meterId, intervalSeconds: 5);

        Assert.Equal(expectedKwh, reading.Kwh);
    }

    [Fact]
    public void Generate_ProducesUniqueReadingIds()
    {
        var generator = new MeterReadingGenerator(new HouseholdPowerDataset());

        var first = generator.Generate("MTR-100-Elm St", intervalSeconds: 5);
        var second = generator.Generate("MTR-100-Elm St", intervalSeconds: 5);

        Assert.NotEqual(first.ReadingId, second.ReadingId);
    }

    [Fact]
    public void Generate_SetsTimestampCloseToNow()
    {
        var generator = new MeterReadingGenerator(new HouseholdPowerDataset());
        var before = DateTimeOffset.UtcNow;

        var reading = generator.Generate("MTR-100-Elm St", intervalSeconds: 5);

        var after = DateTimeOffset.UtcNow;
        Assert.InRange(reading.Timestamp, before, after);
    }
}
