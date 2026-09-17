using Microsoft.EntityFrameworkCore;

namespace GridPulse.GridOperations;

public sealed class WorkOrderCorrelationProcessor(
    GridOperationsDbContext dbContext,
    OutageCorrelator correlator,
    WorkOrderStatusTransitioner transitioner)
{
    public async Task ProcessAsync(DetectedAnomaly anomaly, CancellationToken cancellationToken)
    {
        var openOutages = await dbContext.Outages
            .Where(o => o.Status != "Restored")
            .ToListAsync(cancellationToken);

        var openDrafts = (await dbContext.WorkOrders
                .Where(w => w.OutageId == null)
                .ToListAsync(cancellationToken))
            .Where(w => !transitioner.IsTerminal(w.Status))
            .ToList();

        var result = correlator.Correlate(anomaly, openOutages, openDrafts);

        switch (result.Outcome)
        {
            case CorrelationOutcome.AlreadyCovered:
                return;

            case CorrelationOutcome.CreateDraft:
                dbContext.WorkOrders.Add(new WorkOrder
                {
                    Id = Guid.NewGuid(),
                    HazardType = WorkOrderHazardTypes.SuspectedOutage,
                    MeterId = anomaly.MeterId,
                    AccountId = anomaly.AccountId,
                    StreetName = anomaly.StreetName,
                    StreetNumber = anomaly.StreetNumber,
                    Description = $"Auto-detected: meter {anomaly.MeterId} stopped transmitting readings.",
                    Status = "Reported",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                break;

            case CorrelationOutcome.MergeIntoExistingDraft:
                var existingDraft = result.ExistingDraft!;
                var outage = new Outage
                {
                    Id = Guid.NewGuid(),
                    StreetName = anomaly.StreetName,
                    StreetNumberRangeStart = Math.Min(existingDraft.StreetNumber!.Value, anomaly.StreetNumber),
                    StreetNumberRangeEnd = Math.Max(existingDraft.StreetNumber!.Value, anomaly.StreetNumber),
                    DetectedAt = DateTimeOffset.UtcNow,
                    Status = "Suspected"
                };
                dbContext.Outages.Add(outage);

                existingDraft.OutageId = outage.Id;
                existingDraft.UpdatedAt = DateTimeOffset.UtcNow;
                break;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
