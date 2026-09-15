using System.Xml.Linq;

namespace GridPulse.UsageAggregation.Espi;

public static class EspiConstants
{
    public static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";
    public static readonly XNamespace EspiNamespace = "http://naesb.org/espi";

    public const string RoutePrefix = "/espi/1_1/resource";
    public const string AtomContentType = "application/atom+xml; charset=utf-8";
    public const string MeterReadingId = "1";

    // Real NAESB ESPI enum codes - GridPulse only ever produces one shape of
    // data (residential electricity consumption, hourly), so these are fixed
    // constants rather than per-account configuration.
    public const int ServiceCategoryKindElectricity = 0;
    public const int ReadingTypeKindEnergy = 12;
    public const int CommodityElectricitySecondaryMetered = 1;
    public const int FlowDirectionForward = 1;
    public const int AccumulationBehaviourDeltaData = 4;
    public const int UnitOfMeasureWattHours = 72;
    public const int PowerOfTenMultiplierNone = 0;
}
