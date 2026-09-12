namespace GridPulse.MeterSimulator;

public sealed class MeterSimulatorOptions
{
    public const string SectionName = "MeterSimulator";

    public int IntervalSeconds { get; set; } = 5;

    public IReadOnlyList<string> MeterIds { get; set; } =
    [
        // even side
        "MTR-100-MAIN-ST", "MTR-102-MAIN-ST", "MTR-104-MAIN-ST", "MTR-106-MAIN-ST",
        "MTR-108-MAIN-ST", "MTR-110-MAIN-ST", "MTR-112-MAIN-ST", "MTR-114-MAIN-ST",
        "MTR-116-MAIN-ST", "MTR-118-MAIN-ST", "MTR-120-MAIN-ST", "MTR-122-MAIN-ST",
        "MTR-124-MAIN-ST", "MTR-126-MAIN-ST",
        // odd side (across the street)
        "MTR-101-MAIN-ST", "MTR-103-MAIN-ST", "MTR-105-MAIN-ST", "MTR-107-MAIN-ST",
        "MTR-109-MAIN-ST", "MTR-111-MAIN-ST", "MTR-113-MAIN-ST", "MTR-115-MAIN-ST",
        "MTR-117-MAIN-ST", "MTR-119-MAIN-ST", "MTR-121-MAIN-ST", "MTR-123-MAIN-ST",
        "MTR-125-MAIN-ST", "MTR-127-MAIN-ST"
    ];
}
