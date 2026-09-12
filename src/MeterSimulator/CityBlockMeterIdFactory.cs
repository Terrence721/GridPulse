namespace GridPulse.MeterSimulator;

public sealed class CityBlockMeterIdFactory
{
    public IReadOnlyList<string> Create(MeterSimulatorOptions options)
    {
        var ids = new List<string>(options.BuildingsPerSide * 2);

        for (var i = 0; i < options.BuildingsPerSide; i++)
        {
            var evenAddress = options.StartingAddress + i * 2;
            var oddAddress = evenAddress + 1;

            ids.Add($"MTR-{evenAddress}-{options.StreetName}");
            ids.Add($"MTR-{oddAddress}-{options.StreetName}");
        }

        return ids;
    }
}
