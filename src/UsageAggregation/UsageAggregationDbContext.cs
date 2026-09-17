using Microsoft.EntityFrameworkCore;

namespace GridPulse.UsageAggregation;

public sealed class UsageAggregationDbContext(DbContextOptions<UsageAggregationDbContext> options) : DbContext(options)
{
    public DbSet<ProcessedReading> ProcessedReadings => Set<ProcessedReading>();

    public DbSet<HourlyUsage> HourlyUsages => Set<HourlyUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedReading>()
            .HasKey(r => r.ReadingId);

        modelBuilder.Entity<ProcessedReading>()
            .HasIndex(r => new { r.MeterId, r.Timestamp });

        modelBuilder.Entity<HourlyUsage>()
            .HasIndex(u => new { u.AccountId, u.PeriodStart })
            .IsUnique();
    }
}
