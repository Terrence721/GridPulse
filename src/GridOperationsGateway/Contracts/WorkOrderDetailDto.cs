namespace GridPulse.GridOperationsGateway.Contracts;

public sealed record WorkOrderDetailDto(WorkOrderDto WorkOrder, OutageDto? Outage);
