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

        // Nothing else in the codebase ever moves an Outage off its "Suspected" default -
        // without this, WorkOrderCorrelationProcessor's `o.Status != "Restored"` query would
        // treat every outage as open forever, so a genuinely new anomaly at the same address
        // range would be silently folded into a long-since-fixed incident instead of opening
        // a new one.
        if (newStatus == "Restored" && workOrder.OutageId is { } outageId)
        {
            var outage = await dbContext.Outages.FindAsync([outageId], cancellationToken);
            if (outage is not null)
            {
                outage.Status = "Restored";
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
