using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridPulse.GridOperations;

public sealed class GridOperationsDbContextFactory : IDesignTimeDbContextFactory<GridOperationsDbContext>
{
    public GridOperationsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GridOperationsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=gridoperationsdb");

        return new GridOperationsDbContext(optionsBuilder.Options);
    }
}
