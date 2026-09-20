using System.Net;

using GridPulse.GridOperationsGateway.Contracts;

namespace GridPulse.GridOperationsGateway;

public sealed class GridOperationsClient(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("grid-operations");

    public Task<List<WorkOrderDto>?> GetWorkOrdersAsync(CancellationToken ct) =>
        Client.GetFromJsonAsync<List<WorkOrderDto>>("/work-orders", ct);

    public Task<WorkOrderDto?> GetWorkOrderAsync(Guid id, CancellationToken ct) =>
        GetOrNullAsync<WorkOrderDto>($"/work-orders/{id}", ct);

    public Task<OutageDto?> GetOutageAsync(Guid id, CancellationToken ct) =>
        GetOrNullAsync<OutageDto>($"/outages/{id}", ct);

    public Task<List<OutageDto>?> GetOutagesAsync(CancellationToken ct) =>
        Client.GetFromJsonAsync<List<OutageDto>>("/outages", ct);

    public Task<HttpResponseMessage> CreateWorkOrderAsync(CreateWorkOrderRequest request, CancellationToken ct) =>
        Client.PostAsJsonAsync("/work-orders", request, ct);

    public Task<HttpResponseMessage> UpdateStatusAsync(Guid id, UpdateWorkOrderStatusRequest request, CancellationToken ct) =>
        Client.PatchAsJsonAsync($"/work-orders/{id}/status", request, ct);

    // Confirmed live (real HTTP round-trip test): GetFromJsonAsync throws
    // HttpRequestException on any non-success status, including 404, rather
    // than returning null - callers wanting 404-to-null passthrough
    // (matching GridOperations' own Results.NotFound()) need this instead.
    private async Task<T?> GetOrNullAsync<T>(string requestUri, CancellationToken ct) where T : class
    {
        try
        {
            return await Client.GetFromJsonAsync<T>(requestUri, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
