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

        // Only auto-detected drafts are ever candidates here - a tech-filed work
        // order (any other HazardType) must never be silently merged or deleted
        // by this background process.
        var openDrafts = (await dbContext.WorkOrders
                .Where(w => w.OutageId == null && w.HazardType == WorkOrderHazardTypes.SuspectedOutage)
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

            case CorrelationOutcome.CreateOutage:
            {
                var nearbyDrafts = result.NearbyDrafts!;
                var minNumber = Math.Min(anomaly.StreetNumber, nearbyDrafts.Min(d => d.StreetNumber!.Value));
                var maxNumber = Math.Max(anomaly.StreetNumber, nearbyDrafts.Max(d => d.StreetNumber!.Value));

                var outage = new Outage
                {
                    Id = Guid.NewGuid(),
                    StreetName = anomaly.StreetName,
                    StreetNumberRangeStart = minNumber,
                    StreetNumberRangeEnd = maxNumber,
                    DetectedAt = DateTimeOffset.UtcNow,
                    Status = "Suspected"
                };
                dbContext.Outages.Add(outage);

                // Exactly one WorkOrder survives per Outage - keep the earliest
                // draft (first detected) and remove the rest as redundant, rather
                // than flooding a tech with duplicate line items for one incident.
                var keptDraft = nearbyDrafts.OrderBy(d => d.CreatedAt).First();
                keptDraft.OutageId = outage.Id;
                keptDraft.UpdatedAt = DateTimeOffset.UtcNow;

                foreach (var redundant in nearbyDrafts.Where(d => d != keptDraft))
                {
                    dbContext.WorkOrders.Remove(redundant);
                }

                break;
            }

            case CorrelationOutcome.ExtendOutage:
            {
                var outage = result.Outage!;
                outage.StreetNumberRangeStart = Math.Min(outage.StreetNumberRangeStart, anomaly.StreetNumber);
                outage.StreetNumberRangeEnd = Math.Max(outage.StreetNumberRangeEnd, anomaly.StreetNumber);

                // Any orphaned draft now inside the widened range is already
                // represented by this Outage's one surviving WorkOrder.
                foreach (var absorbed in result.NearbyDrafts!)
                {
                    dbContext.WorkOrders.Remove(absorbed);
                }

                break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
