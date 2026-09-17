using Microsoft.Extensions.Options;

namespace GridPulse.GridOperations.Tests;

public sealed class OutageCorrelatorTests
{
    private static OutageCorrelator CreateCorrelator(int addressWindow = 10, int timeWindowSeconds = 60) =>
        new(Options.Create(new GridOperationsOptions
        {
            CorrelationAddressWindow = addressWindow,
            CorrelationTimeWindowSeconds = timeWindowSeconds
        }));

    private static DetectedAnomaly CreateAnomaly(string meterId = "MTR-100-Elm St", string streetName = "Elm St", int streetNumber = 100, DateTimeOffset? lastSeenAt = null) =>
        new(meterId, "acct-1", streetName, streetNumber, lastSeenAt ?? DateTimeOffset.UtcNow);

    private static WorkOrder CreateDraft(string meterId, string streetName, int streetNumber, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        HazardType = WorkOrderHazardTypes.SuspectedOutage,
        MeterId = meterId,
        StreetName = streetName,
        StreetNumber = streetNumber,
        Description = "Auto-detected",
        Status = "Reported",
        CreatedAt = createdAt,
        UpdatedAt = createdAt
    };

    [Fact]
    public void Correlate_NoExistingState_ReturnsCreateDraft()
    {
        var correlator = CreateCorrelator();
        var result = correlator.Correlate(CreateAnomaly(), [], []);

        Assert.Equal(CorrelationOutcome.CreateDraft, result.Outcome);
    }

    [Fact]
    public void Correlate_ExistingDraftForSameMeter_ReturnsAlreadyCovered()
    {
        var correlator = CreateCorrelator();
        var anomaly = CreateAnomaly(meterId: "MTR-100-Elm St");
        var existingDraft = CreateDraft("MTR-100-Elm St", "Elm St", 100, DateTimeOffset.UtcNow);

        var result = correlator.Correlate(anomaly, [], [existingDraft]);

        Assert.Equal(CorrelationOutcome.AlreadyCovered, result.Outcome);
    }

    [Fact]
    public void Correlate_ExistingOutageCoversStreetNumber_ReturnsAlreadyCovered()
    {
        var correlator = CreateCorrelator();
        var anomaly = CreateAnomaly(streetNumber: 104);
        var outage = new Outage
        {
            Id = Guid.NewGuid(),
            StreetName = "Elm St",
            StreetNumberRangeStart = 100,
            StreetNumberRangeEnd = 106,
            DetectedAt = DateTimeOffset.UtcNow,
            Status = "Suspected"
        };

        var result = correlator.Correlate(anomaly, [outage], []);

        Assert.Equal(CorrelationOutcome.AlreadyCovered, result.Outcome);
    }

    [Fact]
    public void Correlate_NearbyDraftWithinWindow_ReturnsCreateOutage()
    {
        var correlator = CreateCorrelator(addressWindow: 10, timeWindowSeconds: 60);
        var now = DateTimeOffset.UtcNow;
        var existingDraft = CreateDraft("MTR-100-Elm St", "Elm St", 100, now);
        var anomaly = CreateAnomaly(meterId: "MTR-104-Elm St", streetNumber: 104, lastSeenAt: now.AddSeconds(10));

        var result = correlator.Correlate(anomaly, [], [existingDraft]);

        Assert.Equal(CorrelationOutcome.CreateOutage, result.Outcome);
        Assert.Contains(existingDraft, result.NearbyDrafts!);
    }

    [Fact]
    public void Correlate_NearbyOutageWithinWindow_ReturnsExtendOutage()
    {
        var correlator = CreateCorrelator(addressWindow: 10, timeWindowSeconds: 60);
        var outage = new Outage
        {
            Id = Guid.NewGuid(),
            StreetName = "Elm St",
            StreetNumberRangeStart = 100,
            StreetNumberRangeEnd = 104,
            DetectedAt = DateTimeOffset.UtcNow,
            Status = "Suspected"
        };
        var anomaly = CreateAnomaly(meterId: "MTR-108-Elm St", streetNumber: 108);

        var result = correlator.Correlate(anomaly, [outage], []);

        Assert.Equal(CorrelationOutcome.ExtendOutage, result.Outcome);
        Assert.Same(outage, result.Outage);
    }

    [Fact]
    public void Correlate_NearbyOutageWithOrphanedDraftInWidenedRange_AbsorbsTheOrphan()
    {
        var correlator = CreateCorrelator(addressWindow: 10, timeWindowSeconds: 60);
        var now = DateTimeOffset.UtcNow;
        var outage = new Outage
        {
            Id = Guid.NewGuid(),
            StreetName = "Elm St",
            StreetNumberRangeStart = 100,
            StreetNumberRangeEnd = 108,
            DetectedAt = now,
            Status = "Suspected"
        };
        var orphanedDraft = CreateDraft("MTR-104-Elm St", "Elm St", 104, now);
        var anomaly = CreateAnomaly(meterId: "MTR-112-Elm St", streetNumber: 112, lastSeenAt: now);

        var result = correlator.Correlate(anomaly, [outage], [orphanedDraft]);

        Assert.Equal(CorrelationOutcome.ExtendOutage, result.Outcome);
        Assert.Contains(orphanedDraft, result.NearbyDrafts!);
    }

    [Fact]
    public void Correlate_DraftOutsideAddressWindow_ReturnsCreateDraft()
    {
        var correlator = CreateCorrelator(addressWindow: 10, timeWindowSeconds: 60);
        var now = DateTimeOffset.UtcNow;
        var existingDraft = CreateDraft("MTR-100-Elm St", "Elm St", 100, now);
        var anomaly = CreateAnomaly(meterId: "MTR-200-Elm St", streetNumber: 200, lastSeenAt: now);

        var result = correlator.Correlate(anomaly, [], [existingDraft]);

        Assert.Equal(CorrelationOutcome.CreateDraft, result.Outcome);
    }

    [Fact]
    public void Correlate_DraftOutsideTimeWindow_ReturnsCreateDraft()
    {
        var correlator = CreateCorrelator(addressWindow: 10, timeWindowSeconds: 60);
        var now = DateTimeOffset.UtcNow;
        var existingDraft = CreateDraft("MTR-100-Elm St", "Elm St", 100, now.AddMinutes(-10));
        var anomaly = CreateAnomaly(meterId: "MTR-104-Elm St", streetNumber: 104, lastSeenAt: now);

        var result = correlator.Correlate(anomaly, [], [existingDraft]);

        Assert.Equal(CorrelationOutcome.CreateDraft, result.Outcome);
    }

    [Fact]
    public void Correlate_DraftOnDifferentStreet_ReturnsCreateDraft()
    {
        var correlator = CreateCorrelator();
        var now = DateTimeOffset.UtcNow;
        var existingDraft = CreateDraft("MTR-100-Elm St", "Elm St", 100, now);
        var anomaly = CreateAnomaly(meterId: "MTR-102-Oak St", streetName: "Oak St", streetNumber: 102, lastSeenAt: now);

        var result = correlator.Correlate(anomaly, [], [existingDraft]);

        Assert.Equal(CorrelationOutcome.CreateDraft, result.Outcome);
    }
}
