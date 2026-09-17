namespace GridPulse.AccountCustomer;

public sealed record RegisterMeterRequest(string MeterId, Guid AccountId, string StreetName, int StreetNumber);
