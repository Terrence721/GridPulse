using Microsoft.EntityFrameworkCore;

namespace GridPulse.Billing;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => new { i.MeterId, i.PeriodStart, i.PeriodEnd, i.RatePlanType })
            .IsUnique();
    }
}
