namespace GridPulse.Shared;

public sealed record MeterSeedInfo(string MeterId, string StreetName, int StreetNumber);

public sealed class CityBlockMeterIdFactory
{
    public IReadOnlyList<MeterSeedInfo> Create(string streetName, int startingAddress, int buildingsPerSide)
    {
        var meters = new List<MeterSeedInfo>(buildingsPerSide * 2);

        for (var i = 0; i < buildingsPerSide; i++)
        {
            var evenAddress = startingAddress + i * 2;
            var oddAddress = evenAddress + 1;

            meters.Add(BuildMeter(evenAddress, streetName));
            meters.Add(BuildMeter(oddAddress, streetName));
        }

        return meters;
    }

    private static MeterSeedInfo BuildMeter(int streetNumber, string streetName) =>
        new($"MTR-{streetNumber}-{streetName}", streetName, streetNumber);
}
