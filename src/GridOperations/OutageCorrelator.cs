using Microsoft.Extensions.Options;

namespace GridPulse.GridOperations;

public enum CorrelationOutcome
{
    AlreadyCovered,
    CreateDraft,
    MergeIntoExistingDraft
}

public sealed record CorrelationResult(CorrelationOutcome Outcome, WorkOrder? ExistingDraft = null);

public sealed class OutageCorrelator(IOptions<GridOperationsOptions> options)
{
    public CorrelationResult Correlate(DetectedAnomaly anomaly, IReadOnlyList<Outage> openOutages, IReadOnlyList<WorkOrder> openDraftWorkOrders)
    {
        var alreadyCoveredByOutage = openOutages.Any(o =>
            o.StreetName == anomaly.StreetName &&
            anomaly.StreetNumber >= o.StreetNumberRangeStart &&
            anomaly.StreetNumber <= o.StreetNumberRangeEnd);

        if (alreadyCoveredByOutage)
        {
            return new CorrelationResult(CorrelationOutcome.AlreadyCovered);
        }

        var alreadyCoveredByDraft = openDraftWorkOrders.Any(w => w.MeterId == anomaly.MeterId);
        if (alreadyCoveredByDraft)
        {
            return new CorrelationResult(CorrelationOutcome.AlreadyCovered);
        }

        var nearbyDraft = openDraftWorkOrders.FirstOrDefault(w =>
            w.StreetName == anomaly.StreetName &&
            w.StreetNumber is not null &&
            Math.Abs(w.StreetNumber.Value - anomaly.StreetNumber) <= options.Value.CorrelationAddressWindow &&
            Math.Abs((w.CreatedAt - anomaly.LastSeenAt).TotalSeconds) <= options.Value.CorrelationTimeWindowSeconds);

        return nearbyDraft is not null
            ? new CorrelationResult(CorrelationOutcome.MergeIntoExistingDraft, nearbyDraft)
            : new CorrelationResult(CorrelationOutcome.CreateDraft);
    }
}
