using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GridPulse.Shared;

namespace GridPulse.AccountCustomer.Tests;

public sealed class AccountSeederTests
{
    private static AccountCustomerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountCustomerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AccountCustomerDbContext(options);
    }

    private static AccountSeedOptions CreateSeedOptions() => new()
    {
        StreetName = "Elm St",
        StartingAddress = 10,
        BuildingsPerSide = 2,
        NotificationWebhookUrl = "http://localhost:9999/webhook"
    };

    [Fact]
    public async Task SeedAsync_CreatesOneAccountAndMeterPerConfiguredMeter()
    {
        await using var dbContext = CreateDbContext();
        var seeder = new AccountSeeder(dbContext, Options.Create(CreateSeedOptions()), new CityBlockMeterIdFactory());

        await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(4, dbContext.Meters.Count());
        Assert.Equal(4, dbContext.Accounts.Count());
    }

    [Fact]
    public async Task SeedAsync_SetsContactWebhookUrlFromOptions()
    {
        await using var dbContext = CreateDbContext();
        var seeder = new AccountSeeder(dbContext, Options.Create(CreateSeedOptions()), new CityBlockMeterIdFactory());

        await seeder.SeedAsync(CancellationToken.None);

        Assert.All(dbContext.Accounts, account => Assert.Equal("http://localhost:9999/webhook", account.ContactWebhookUrl));
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_DoesNotDuplicate()
    {
        await using var dbContext = CreateDbContext();
        var seeder = new AccountSeeder(dbContext, Options.Create(CreateSeedOptions()), new CityBlockMeterIdFactory());

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(4, dbContext.Meters.Count());
        Assert.Equal(4, dbContext.Accounts.Count());
    }

    [Fact]
    public async Task SeedAsync_EachMeterMapsToARealAccount()
    {
        await using var dbContext = CreateDbContext();
        var seeder = new AccountSeeder(dbContext, Options.Create(CreateSeedOptions()), new CityBlockMeterIdFactory());

        await seeder.SeedAsync(CancellationToken.None);

        var accountIds = dbContext.Accounts.Select(a => a.Id).ToHashSet();
        Assert.All(dbContext.Meters, meter => Assert.Contains(meter.AccountId, accountIds));
    }
}
