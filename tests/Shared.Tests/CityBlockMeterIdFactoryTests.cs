namespace GridPulse.Shared.Tests;

public sealed class CityBlockMeterIdFactoryTests
{
    [Fact]
    public void Create_ReturnsTwoMetersPerBuilding()
    {
        var meters = new CityBlockMeterIdFactory().Create("Elm St", 10, 2);

        Assert.Equal(4, meters.Count);
    }

    [Fact]
    public void Create_GeneratesConsecutiveAddressesStartingAtStartingAddress()
    {
        var meters = new CityBlockMeterIdFactory().Create("Elm St", 10, 2);

        Assert.Equal(
            ["MTR-10-Elm St", "MTR-11-Elm St", "MTR-12-Elm St", "MTR-13-Elm St"],
            meters.Select(m => m.MeterId));
        Assert.Equal([10, 11, 12, 13], meters.Select(m => m.StreetNumber));
        Assert.All(meters, m => Assert.Equal("Elm St", m.StreetName));
    }

    [Fact]
    public void Create_ReturnsOnlyUniqueMeterIds()
    {
        var meters = new CityBlockMeterIdFactory().Create("Elm St", 100, 14);

        Assert.Equal(meters.Count, meters.Select(m => m.MeterId).Distinct().Count());
    }
}
