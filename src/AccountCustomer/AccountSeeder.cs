using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GridPulse.Shared;

namespace GridPulse.AccountCustomer;

public sealed class AccountSeeder(
    AccountCustomerDbContext dbContext,
    IOptions<AccountSeedOptions> seedOptions,
    CityBlockMeterIdFactory meterIdFactory)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var meters = meterIdFactory.Create(seedOptions.Value.StreetName, seedOptions.Value.StartingAddress, seedOptions.Value.BuildingsPerSide);

        foreach (var meter in meters)
        {
            var alreadyRegistered = await dbContext.Meters.AnyAsync(m => m.MeterId == meter.MeterId, cancellationToken);
            if (alreadyRegistered)
            {
                continue;
            }

            var account = new Account { Id = Guid.NewGuid(), ContactWebhookUrl = seedOptions.Value.NotificationWebhookUrl };
            dbContext.Accounts.Add(account);
            dbContext.Meters.Add(new Meter { MeterId = meter.MeterId, AccountId = account.Id, StreetName = meter.StreetName, StreetNumber = meter.StreetNumber });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
