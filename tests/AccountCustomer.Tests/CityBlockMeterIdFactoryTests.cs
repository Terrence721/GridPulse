namespace GridPulse.AccountCustomer.Tests;

public sealed class CityBlockMeterIdFactoryTests
{
    [Fact]
    public void Create_ReturnsTwoIdsPerBuilding()
    {
        var options = new AccountSeedOptions
        {
            StreetName = "Elm St",
            StartingAddress = 10,
            BuildingsPerSide = 2,
            NotificationWebhookUrl = "http://localhost:9999/webhook"
        };

        var ids = new CityBlockMeterIdFactory().Create(options);

        Assert.Equal(4, ids.Count);
    }

    [Fact]
    public void Create_GeneratesConsecutiveAddressesStartingAtStartingAddress()
    {
        var options = new AccountSeedOptions
        {
            StreetName = "Elm St",
            StartingAddress = 10,
            BuildingsPerSide = 2,
            NotificationWebhookUrl = "http://localhost:9999/webhook"
        };

        var ids = new CityBlockMeterIdFactory().Create(options);

        Assert.Equal(
            ["MTR-10-Elm St", "MTR-11-Elm St", "MTR-12-Elm St", "MTR-13-Elm St"],
            ids);
    }

    [Fact]
    public void Create_ReturnsOnlyUniqueIds()
    {
        var options = new AccountSeedOptions
        {
            StreetName = "Elm St",
            StartingAddress = 100,
            BuildingsPerSide = 14,
            NotificationWebhookUrl = "http://localhost:9999/webhook"
        };

        var ids = new CityBlockMeterIdFactory().Create(options);

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
