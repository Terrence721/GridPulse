using Microsoft.Extensions.Options;

namespace GridPulse.GridOperations;

public enum CorrelationOutcome
{
    AlreadyCovered,
    CreateDraft,
    CreateOutage,
    ExtendOutage
}

public sealed record CorrelationResult(
    CorrelationOutcome Outcome,
    Outage? Outage = null,
    IReadOnlyList<WorkOrder>? NearbyDrafts = null);

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

        var addressWindow = options.Value.CorrelationAddressWindow;
        var timeWindowSeconds = options.Value.CorrelationTimeWindowSeconds;

        var nearbyOutage = openOutages.FirstOrDefault(o =>
            o.StreetName == anomaly.StreetName &&
            anomaly.StreetNumber >= o.StreetNumberRangeStart - addressWindow &&
            anomaly.StreetNumber <= o.StreetNumberRangeEnd + addressWindow);

        var nearbyDrafts = openDraftWorkOrders.Where(w =>
            w.StreetName == anomaly.StreetName &&
            w.StreetNumber is not null &&
            Math.Abs(w.StreetNumber.Value - anomaly.StreetNumber) <= addressWindow &&
            Math.Abs((w.CreatedAt - anomaly.LastSeenAt).TotalSeconds) <= timeWindowSeconds)
            .ToList();

        if (nearbyOutage is not null)
        {
            return new CorrelationResult(CorrelationOutcome.ExtendOutage, nearbyOutage, nearbyDrafts);
        }

        return nearbyDrafts.Count > 0
            ? new CorrelationResult(CorrelationOutcome.CreateOutage, NearbyDrafts: nearbyDrafts)
            : new CorrelationResult(CorrelationOutcome.CreateDraft);
    }
}
