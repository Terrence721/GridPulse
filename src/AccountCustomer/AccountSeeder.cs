using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridPulse.AccountCustomer;

public sealed class AccountSeeder(
    AccountCustomerDbContext dbContext,
    IOptions<AccountSeedOptions> seedOptions,
    CityBlockMeterIdFactory meterIdFactory)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var meterIds = meterIdFactory.Create(seedOptions.Value);

        foreach (var meterId in meterIds)
        {
            var alreadyRegistered = await dbContext.Meters.AnyAsync(m => m.MeterId == meterId, cancellationToken);
            if (alreadyRegistered)
            {
                continue;
            }

            var account = new Account { Id = Guid.NewGuid(), ContactWebhookUrl = seedOptions.Value.NotificationWebhookUrl };
            dbContext.Accounts.Add(account);
            dbContext.Meters.Add(new Meter { MeterId = meterId, AccountId = account.Id });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
