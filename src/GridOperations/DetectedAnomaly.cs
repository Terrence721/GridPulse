namespace GridPulse.GridOperations;

public sealed record DetectedAnomaly(string MeterId, string AccountId, string StreetName, int StreetNumber, DateTimeOffset LastSeenAt);
