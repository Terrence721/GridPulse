using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridPulse.AccountCustomer;

public sealed class AccountCustomerDbContextFactory : IDesignTimeDbContextFactory<AccountCustomerDbContext>
{
    public AccountCustomerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountCustomerDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=accountsdb");

        return new AccountCustomerDbContext(optionsBuilder.Options);
    }
}
