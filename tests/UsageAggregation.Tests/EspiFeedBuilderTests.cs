using System.Xml.Linq;
using GridPulse.UsageAggregation.Espi;
using static GridPulse.UsageAggregation.Espi.EspiConstants;

namespace GridPulse.UsageAggregation.Tests;

public sealed class EspiFeedBuilderTests
{
    private static readonly DateTimeOffset GeneratedAt = new(2026, 9, 15, 14, 0, 0, TimeSpan.Zero);
    private const string BaseUrl = "https://host";

    [Fact]
    public void BuildUsagePointCollectionFeed_TwoAccounts_HasOneEntryPerAccountWithCorrectKindAndSelfLink()
    {
        var feed = EspiFeedBuilder.BuildUsagePointCollectionFeed(BaseUrl, ["ACC-1", "ACC-2"], GeneratedAt);

        var entries = feed.Root!.Elements(AtomNamespace + "entry").ToList();
        Assert.Equal(2, entries.Count);

        Assert.Equal("urn:gridpulse:usage-point:ACC-1", entries[0].Element(AtomNamespace + "id")!.Value);
        Assert.Equal("urn:gridpulse:usage-point:ACC-2", entries[1].Element(AtomNamespace + "id")!.Value);
        foreach (var entry in entries)
        {
            Assert.Equal("0", entry.Descendants(EspiNamespace + "kind").Single().Value);
        }

        var selfLink = feed.Root!.Elements(AtomNamespace + "link").Single(l => l.Attribute("rel")!.Value == "self");
        Assert.Equal($"{BaseUrl}{RoutePrefix}/UsagePoint", selfLink.Attribute("href")!.Value);
    }

    [Fact]
    public void BuildUsagePointFeed_SingleAccount_HasCorrectContentAndSelfLink()
    {
        var feed = EspiFeedBuilder.BuildUsagePointFeed(BaseUrl, "ACC-1", GeneratedAt);

        var entry = feed.Root!.Elements(AtomNamespace + "entry").Single();
        Assert.Equal("0", entry.Descendants(EspiNamespace + "kind").Single().Value);

        var selfLink = feed.Root!.Elements(AtomNamespace + "link").Single(l => l.Attribute("rel")!.Value == "self");
        Assert.Equal($"{BaseUrl}{RoutePrefix}/UsagePoint/ACC-1", selfLink.Attribute("href")!.Value);
    }

    [Fact]
    public void BuildMeterReadingFeed_SingleAccount_HasRelatedLinksToReadingTypeAndIntervalBlock()
    {
        var feed = EspiFeedBuilder.BuildMeterReadingFeed(BaseUrl, "ACC-1", GeneratedAt);

        var entry = feed.Root!.Elements(AtomNamespace + "entry").Single();
        var relatedLinks = entry.Elements(AtomNamespace + "link")
            .Where(l => l.Attribute("rel")!.Value == "related")
            .Select(l => l.Attribute("href")!.Value)
            .ToList();

        var meterReadingBase = $"{BaseUrl}{RoutePrefix}/UsagePoint/ACC-1/MeterReading/{MeterReadingId}";
        Assert.Contains($"{meterReadingBase}/ReadingType", relatedLinks);
        Assert.Contains($"{meterReadingBase}/IntervalBlock", relatedLinks);

        var content = entry.Element(AtomNamespace + "content")!;
        Assert.NotNull(content.Element(EspiNamespace + "MeterReading"));
    }

    [Fact]
    public void BuildReadingTypeFeed_ReturnsAllSevenFieldsInRealSchemaOrderWithCorrectValues()
    {
        var feed = EspiFeedBuilder.BuildReadingTypeFeed(BaseUrl, "ACC-1", GeneratedAt);

        var readingType = feed.Root!.Descendants(EspiNamespace + "ReadingType").Single();
        var fieldNames = readingType.Elements().Select(e => e.Name.LocalName).ToList();

        Assert.Equal(
            ["accumulationBehaviour", "commodity", "flowDirection", "intervalLength", "kind", "powerOfTenMultiplier", "uom"],
            fieldNames);

        Assert.Equal("4", readingType.Element(EspiNamespace + "accumulationBehaviour")!.Value);
        Assert.Equal("1", readingType.Element(EspiNamespace + "commodity")!.Value);
        Assert.Equal("1", readingType.Element(EspiNamespace + "flowDirection")!.Value);
        Assert.Equal("3600", readingType.Element(EspiNamespace + "intervalLength")!.Value);
        Assert.Equal("12", readingType.Element(EspiNamespace + "kind")!.Value);
        Assert.Equal("0", readingType.Element(EspiNamespace + "powerOfTenMultiplier")!.Value);
        Assert.Equal("72", readingType.Element(EspiNamespace + "uom")!.Value);
    }

    [Fact]
    public void BuildIntervalBlockFeed_ThreeReadings_ProducesOneBlockWithThreeReadingsInOrderAndCorrectValues()
    {
        var start = new DateTimeOffset(2026, 9, 12, 17, 0, 0, TimeSpan.Zero);
        var readings = new List<HourlyUsage>
        {
            new() { AccountId = "ACC-1", PeriodStart = start, PeriodEnd = start.AddHours(1), TotalKwh = 4.25 },
            new() { AccountId = "ACC-1", PeriodStart = start.AddHours(1), PeriodEnd = start.AddHours(2), TotalKwh = 0.3 },
            new() { AccountId = "ACC-1", PeriodStart = start.AddHours(2), PeriodEnd = start.AddHours(3), TotalKwh = 1.0 }
        };

        var feed = EspiFeedBuilder.BuildIntervalBlockFeed(BaseUrl, "ACC-1", readings, GeneratedAt);

        var blocks = feed.Root!.Descendants(EspiNamespace + "IntervalBlock").ToList();
        var intervalReadings = blocks.Single().Elements(EspiNamespace + "IntervalReading").ToList();
        Assert.Single(blocks);
        Assert.Equal(3, intervalReadings.Count);

        var values = intervalReadings.Select(r => long.Parse(r.Element(EspiNamespace + "value")!.Value)).ToList();
        Assert.Equal([4250, 300, 1000], values);
    }

    [Fact]
    public void BuildIntervalBlockFeed_FloatingPointNoiseInTotalKwh_RoundsToExpectedWattHours()
    {
        var start = new DateTimeOffset(2026, 9, 12, 17, 0, 0, TimeSpan.Zero);
        var readings = new List<HourlyUsage>
        {
            new() { AccountId = "ACC-1", PeriodStart = start, PeriodEnd = start.AddHours(1), TotalKwh = 0.1 + 0.2 }
        };

        var feed = EspiFeedBuilder.BuildIntervalBlockFeed(BaseUrl, "ACC-1", readings, GeneratedAt);

        var value = feed.Root!.Descendants(EspiNamespace + "value").Single().Value;
        Assert.Equal("300", value);
    }

    [Fact]
    public void BuildIntervalBlockFeed_OutOfOrderReadings_IntervalSpansFullRangeAndReadingsAreOrdered()
    {
        var start = new DateTimeOffset(2026, 9, 12, 17, 0, 0, TimeSpan.Zero);
        var readings = new List<HourlyUsage>
        {
            new() { AccountId = "ACC-1", PeriodStart = start.AddHours(2), PeriodEnd = start.AddHours(3), TotalKwh = 1.0 },
            new() { AccountId = "ACC-1", PeriodStart = start, PeriodEnd = start.AddHours(1), TotalKwh = 4.25 },
            new() { AccountId = "ACC-1", PeriodStart = start.AddHours(1), PeriodEnd = start.AddHours(2), TotalKwh = 0.3 }
        };

        var feed = EspiFeedBuilder.BuildIntervalBlockFeed(BaseUrl, "ACC-1", readings, GeneratedAt);

        var interval = feed.Root!.Descendants(EspiNamespace + "interval").Single();
        Assert.Equal(start.ToUnixTimeSeconds().ToString(), interval.Element(EspiNamespace + "start")!.Value);
        Assert.Equal("10800", interval.Element(EspiNamespace + "duration")!.Value);

        var orderedStarts = feed.Root!.Descendants(EspiNamespace + "IntervalReading")
            .Select(r => long.Parse(r.Descendants(EspiNamespace + "start").Single().Value))
            .ToList();
        Assert.Equal(
            [start.ToUnixTimeSeconds(), start.AddHours(1).ToUnixTimeSeconds(), start.AddHours(2).ToUnixTimeSeconds()],
            orderedStarts);
    }

    [Fact]
    public void BuildIntervalBlockFeed_KnownTimestamp_MatchesIndependentlyComputedUnixSeconds()
    {
        // 2026-09-12T17:00:00Z, verified independently via `date -u -d "2026-09-12T17:00:00Z" +%s`
        // rather than trusted from DateTimeOffset.ToUnixTimeSeconds() itself, which would be circular.
        var start = new DateTimeOffset(2026, 9, 12, 17, 0, 0, TimeSpan.Zero);
        var readings = new List<HourlyUsage>
        {
            new() { AccountId = "ACC-1", PeriodStart = start, PeriodEnd = start.AddHours(1), TotalKwh = 1.0 }
        };

        var feed = EspiFeedBuilder.BuildIntervalBlockFeed(BaseUrl, "ACC-1", readings, GeneratedAt);

        var timePeriodStart = feed.Root!.Descendants(EspiNamespace + "timePeriod").Single().Element(EspiNamespace + "start")!.Value;
        Assert.Equal("1789232400", timePeriodStart);
    }
}
