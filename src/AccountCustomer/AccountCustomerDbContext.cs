using Microsoft.EntityFrameworkCore;

namespace GridPulse.AccountCustomer;

public sealed class AccountCustomerDbContext(DbContextOptions<AccountCustomerDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Meter> Meters => Set<Meter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meter>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(m => m.AccountId);
    }
}
