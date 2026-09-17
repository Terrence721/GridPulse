using Microsoft.EntityFrameworkCore;

namespace GridPulse.GridOperations;

public sealed class GridOperationsDbContext(DbContextOptions<GridOperationsDbContext> options) : DbContext(options)
{
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Outage> Outages => Set<Outage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>()
            .HasOne<Outage>()
            .WithMany()
            .HasForeignKey(w => w.OutageId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
