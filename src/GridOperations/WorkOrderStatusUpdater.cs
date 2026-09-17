namespace GridPulse.GridOperations;

public sealed class WorkOrderStatusUpdater(GridOperationsDbContext dbContext, WorkOrderStatusTransitioner transitioner)
{
    public async Task<bool> UpdateStatusAsync(WorkOrder workOrder, string newStatus, CancellationToken cancellationToken)
    {
        if (!transitioner.CanTransition(workOrder.Status, newStatus))
        {
            return false;
        }

        workOrder.Status = newStatus;
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
