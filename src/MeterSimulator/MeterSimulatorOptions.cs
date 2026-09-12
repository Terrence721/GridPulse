namespace GridPulse.MeterSimulator;

public sealed class MeterSimulatorOptions
{
    public const string SectionName = "MeterSimulator";

    public int IntervalSeconds { get; set; } = 5;

    public string StreetName { get; set; } = "MAIN-ST";

    public int StartingAddress { get; set; } = 100;

    public int BuildingsPerSide { get; set; } = 14;
}
