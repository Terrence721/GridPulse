using System.Xml.Linq;
using static GridPulse.UsageAggregation.Espi.EspiConstants;

namespace GridPulse.UsageAggregation.Espi;

public static class EspiFeedBuilder
{
    public static XDocument BuildUsagePointCollectionFeed(string baseUrl, IReadOnlyList<string> accountIds, DateTimeOffset generatedAt)
    {
        var entries = accountIds.Select(accountId =>
            BuildEntry($"usage-point:{accountId}", $"UsagePoint {accountId}", generatedAt, BuildUsagePointContent()));

        return BuildFeed("usage-point-collection", "UsagePoints", generatedAt, $"{baseUrl}{RoutePrefix}/UsagePoint", entries);
    }

    public static XDocument BuildUsagePointFeed(string baseUrl, string accountId, DateTimeOffset generatedAt)
    {
        var entry = BuildEntry($"usage-point:{accountId}", $"UsagePoint {accountId}", generatedAt, BuildUsagePointContent());
        return BuildFeed($"usage-point:{accountId}", $"UsagePoint {accountId}", generatedAt, $"{baseUrl}{RoutePrefix}/UsagePoint/{accountId}", [entry]);
    }

    public static XDocument BuildMeterReadingFeed(string baseUrl, string accountId, DateTimeOffset generatedAt)
    {
        var meterReadingBase = $"{baseUrl}{RoutePrefix}/UsagePoint/{accountId}/MeterReading/{MeterReadingId}";
        var links = new[]
        {
            new XElement(AtomNamespace + "link", new XAttribute("rel", "related"), new XAttribute("href", $"{meterReadingBase}/ReadingType")),
            new XElement(AtomNamespace + "link", new XAttribute("rel", "related"), new XAttribute("href", $"{meterReadingBase}/IntervalBlock"))
        };
        var entry = BuildEntry($"meter-reading:{accountId}:{MeterReadingId}", $"MeterReading {MeterReadingId}", generatedAt, BuildMeterReadingContent(), links);
        return BuildFeed($"meter-reading:{accountId}", $"MeterReadings for {accountId}", generatedAt, $"{baseUrl}{RoutePrefix}/UsagePoint/{accountId}/MeterReading", [entry]);
    }

    public static XDocument BuildReadingTypeFeed(string baseUrl, string accountId, DateTimeOffset generatedAt)
    {
        var entry = BuildEntry($"reading-type:{accountId}:{MeterReadingId}", "ReadingType", generatedAt, BuildReadingTypeContent());
        var self = $"{baseUrl}{RoutePrefix}/UsagePoint/{accountId}/MeterReading/{MeterReadingId}/ReadingType";
        return BuildFeed($"reading-type:{accountId}", "ReadingType", generatedAt, self, [entry]);
    }

    public static XDocument BuildIntervalBlockFeed(string baseUrl, string accountId, IReadOnlyList<HourlyUsage> readings, DateTimeOffset generatedAt)
    {
        var entry = BuildEntry($"interval-block:{accountId}", "IntervalBlock", generatedAt, BuildIntervalBlockContent(readings));
        var self = $"{baseUrl}{RoutePrefix}/UsagePoint/{accountId}/MeterReading/{MeterReadingId}/IntervalBlock";
        return BuildFeed($"interval-block:{accountId}", "IntervalBlock", generatedAt, self, [entry]);
    }

    public static string ToXmlString(XDocument document)
    {
        // XmlWriter always reports encoding="utf-16" when writing to a
        // StringBuilder/StringWriter, regardless of XmlWriterSettings.Encoding
        // - the declaration reflects the TextWriter's own .Encoding, not the
        // setting. Sidestepped entirely by prepending the declaration by hand.
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + document.ToString();
    }

    private static XElement BuildUsagePointContent() =>
        new(EspiNamespace + "UsagePoint",
            new XElement(EspiNamespace + "ServiceCategory",
                new XElement(EspiNamespace + "kind", ServiceCategoryKindElectricity)));

    private static XElement BuildMeterReadingContent() =>
        new(EspiNamespace + "MeterReading");

    private static XElement BuildReadingTypeContent() =>
        new(EspiNamespace + "ReadingType",
            new XElement(EspiNamespace + "accumulationBehaviour", AccumulationBehaviourDeltaData),
            new XElement(EspiNamespace + "commodity", CommodityElectricitySecondaryMetered),
            new XElement(EspiNamespace + "flowDirection", FlowDirectionForward),
            // Structural invariant of ReadingProcessor's hourly bucketing, not
            // deployment config - a literal is correct here, not hardcoding.
            new XElement(EspiNamespace + "intervalLength", 3600),
            new XElement(EspiNamespace + "kind", ReadingTypeKindEnergy),
            new XElement(EspiNamespace + "powerOfTenMultiplier", PowerOfTenMultiplierNone),
            new XElement(EspiNamespace + "uom", UnitOfMeasureWattHours));

    private static XElement BuildIntervalBlockContent(IReadOnlyList<HourlyUsage> readings)
    {
        var ordered = readings.OrderBy(r => r.PeriodStart).ToList();

        var intervalDuration = ordered.Count == 0
            ? 0
            : (long)(ordered[^1].PeriodEnd - ordered[0].PeriodStart).TotalSeconds;
        var intervalStart = ordered.Count == 0 ? 0 : ordered[0].PeriodStart.ToUnixTimeSeconds();

        var block = new XElement(EspiNamespace + "IntervalBlock",
            new XElement(EspiNamespace + "interval",
                new XElement(EspiNamespace + "duration", intervalDuration),
                new XElement(EspiNamespace + "start", intervalStart)));

        foreach (var reading in ordered)
        {
            var duration = (long)(reading.PeriodEnd - reading.PeriodStart).TotalSeconds;
            var value = (long)Math.Round(reading.TotalKwh * 1000);

            block.Add(new XElement(EspiNamespace + "IntervalReading",
                new XElement(EspiNamespace + "timePeriod",
                    new XElement(EspiNamespace + "duration", duration),
                    new XElement(EspiNamespace + "start", reading.PeriodStart.ToUnixTimeSeconds())),
                new XElement(EspiNamespace + "value", value)));
        }

        return block;
    }

    private static XElement BuildEntry(string id, string title, DateTimeOffset updated, XElement content, IEnumerable<XElement>? links = null)
    {
        var entry = new XElement(AtomNamespace + "entry", BuildAtomIdentity(id, title, updated));

        if (links is not null)
        {
            foreach (var link in links)
            {
                entry.Add(link);
            }
        }

        entry.Add(new XElement(AtomNamespace + "content", content));
        return entry;
    }

    private static XDocument BuildFeed(string id, string title, DateTimeOffset updated, string selfHref, IEnumerable<XElement> entries)
    {
        var feed = new XElement(AtomNamespace + "feed",
            new XAttribute(XNamespace.Xmlns + "espi", EspiNamespace),
            BuildAtomIdentity(id, title, updated),
            new XElement(AtomNamespace + "link", new XAttribute("rel", "self"), new XAttribute("href", selfHref)));

        foreach (var entry in entries)
        {
            feed.Add(entry);
        }

        return new XDocument(feed);
    }

    // Shared by both <feed> and <entry> - Atom gives both the same
    // id/title/updated identity triple, and BuildFeed/BuildEntry duplicated
    // it verbatim (including the date-format string) before this extraction.
    private static IEnumerable<XElement> BuildAtomIdentity(string id, string title, DateTimeOffset updated)
    {
        return
        [
            new XElement(AtomNamespace + "id", $"urn:gridpulse:{id}"),
            new XElement(AtomNamespace + "title", title),
            new XElement(AtomNamespace + "updated", updated.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"))
        ];
    }
}
