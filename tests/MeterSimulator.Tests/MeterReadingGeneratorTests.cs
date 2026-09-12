namespace GridPulse.MeterSimulator.Tests;

public sealed class MeterReadingGeneratorTests
{
    [Fact]
    public void Generate_UsesProvidedMeterId()
    {
        var generator = new MeterReadingGenerator();

        var reading = generator.Generate("MTR-100-Elm St");

        Assert.Equal("MTR-100-Elm St", reading.MeterId);
    }

    [Fact]
    public void Generate_ProducesKwhWithinExpectedRange()
    {
        var generator = new MeterReadingGenerator();

        var reading = generator.Generate("MTR-100-Elm St");

        Assert.InRange(reading.Kwh, 0, 5);
    }

    [Fact]
    public void Generate_ProducesUniqueReadingIds()
    {
        var generator = new MeterReadingGenerator();

        var first = generator.Generate("MTR-100-Elm St");
        var second = generator.Generate("MTR-100-Elm St");

        Assert.NotEqual(first.ReadingId, second.ReadingId);
    }

    [Fact]
    public void Generate_SetsTimestampCloseToNow()
    {
        var generator = new MeterReadingGenerator();
        var before = DateTimeOffset.UtcNow;

        var reading = generator.Generate("MTR-100-Elm St");

        var after = DateTimeOffset.UtcNow;
        Assert.InRange(reading.Timestamp, before, after);
    }
}
