namespace GridPulse.Billing;

public sealed record GenerateInvoiceRequest(string MeterId, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, string RatePlanType);
