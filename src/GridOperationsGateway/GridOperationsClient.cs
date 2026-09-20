using GridPulse.GridOperationsGateway.Contracts;

namespace GridPulse.GridOperationsGateway;

public sealed class GridOperationsClient(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("grid-operations");

    public Task<List<WorkOrderDto>?> GetWorkOrdersAsync(CancellationToken ct) =>
        Client.GetFromJsonAsync<List<WorkOrderDto>>("/work-orders", ct);

    public Task<WorkOrderDto?> GetWorkOrderAsync(Guid id, CancellationToken ct) =>
        Client.GetFromJsonAsync<WorkOrderDto?>($"/work-orders/{id}", ct);

    public Task<OutageDto?> GetOutageAsync(Guid id, CancellationToken ct) =>
        Client.GetFromJsonAsync<OutageDto?>($"/outages/{id}", ct);

    public Task<List<OutageDto>?> GetOutagesAsync(CancellationToken ct) =>
        Client.GetFromJsonAsync<List<OutageDto>>("/outages", ct);

    public Task<HttpResponseMessage> CreateWorkOrderAsync(CreateWorkOrderRequest request, CancellationToken ct) =>
        Client.PostAsJsonAsync("/work-orders", request, ct);

    public Task<HttpResponseMessage> UpdateStatusAsync(Guid id, UpdateWorkOrderStatusRequest request, CancellationToken ct) =>
        Client.PatchAsJsonAsync($"/work-orders/{id}/status", request, ct);
}
