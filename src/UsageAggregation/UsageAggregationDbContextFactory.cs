using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridPulse.UsageAggregation;

public sealed class UsageAggregationDbContextFactory : IDesignTimeDbContextFactory<UsageAggregationDbContext>
{
    public UsageAggregationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UsageAggregationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=gridpulsedb");

        return new UsageAggregationDbContext(optionsBuilder.Options);
    }
}
