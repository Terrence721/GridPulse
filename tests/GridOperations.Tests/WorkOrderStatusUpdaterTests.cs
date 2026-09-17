using Microsoft.EntityFrameworkCore;

namespace GridPulse.GridOperations.Tests;

public sealed class WorkOrderStatusUpdaterTests
{
    private static GridOperationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GridOperationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new GridOperationsDbContext(options);
    }

    private static WorkOrder CreateWorkOrder(string status = "Reported") => new()
    {
        Id = Guid.NewGuid(),
        HazardType = WorkOrderHazardTypes.DownedWire,
        Description = "Wire down across driveway.",
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_ReturnsTrueAndUpdatesStatus()
    {
        await using var dbContext = CreateDbContext();
        var workOrder = CreateWorkOrder();
        dbContext.WorkOrders.Add(workOrder);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updater = new WorkOrderStatusUpdater(dbContext, new WorkOrderStatusTransitioner());
        var result = await updater.UpdateStatusAsync(workOrder, "Dispatched", TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Equal("Dispatched", workOrder.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ReturnsFalseAndLeavesStatusUnchanged()
    {
        await using var dbContext = CreateDbContext();
        var workOrder = CreateWorkOrder();
        dbContext.WorkOrders.Add(workOrder);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updater = new WorkOrderStatusUpdater(dbContext, new WorkOrderStatusTransitioner());
        var result = await updater.UpdateStatusAsync(workOrder, "Closed", TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.Equal("Reported", workOrder.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_PersistsChangeToDatabase()
    {
        await using var dbContext = CreateDbContext();
        var workOrder = CreateWorkOrder();
        dbContext.WorkOrders.Add(workOrder);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updater = new WorkOrderStatusUpdater(dbContext, new WorkOrderStatusTransitioner());
        await updater.UpdateStatusAsync(workOrder, "Dispatched", TestContext.Current.CancellationToken);

        var reloaded = await dbContext.WorkOrders.FindAsync([workOrder.Id], TestContext.Current.CancellationToken);
        Assert.Equal("Dispatched", reloaded!.Status);
    }
}
