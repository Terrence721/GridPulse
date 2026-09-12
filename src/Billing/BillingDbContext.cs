using Microsoft.EntityFrameworkCore;

namespace GridPulse.Billing;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
}
