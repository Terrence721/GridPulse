using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GridPulse.GridOperationsGateway.Contracts;

namespace GridPulse.GridOperationsGateway;

public sealed class GridOperationsClient(IHttpClientFactory httpClientFactory)
{
    // GridOperations' minimal API serializes responses with the ASP.NET Core Web
    // defaults (camelCase), but GetFromJsonAsync without explicit options deserializes
    // case-sensitively against these PascalCase DTOs - every property would silently
    // bind to its default (null/0/Guid.Empty) with no exception.
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    private HttpClient Client => httpClientFactory.CreateClient("grid-operations");

    public Task<List<WorkOrderDto>?> GetWorkOrdersAsync(CancellationToken ct) =>
        Client.GetFromJsonAsync<List<WorkOrderDto>>("/work-orders", ResponseOptions, ct);

    public Task<WorkOrderDto?> GetWorkOrderAsync(Guid id, CancellationToken ct) =>
        GetOrNullAsync<WorkOrderDto>($"/work-orders/{id}", ct);

    public Task<OutageDto?> GetOutageAsync(Guid id, CancellationToken ct) =>
        GetOrNullAsync<OutageDto>($"/outages/{id}", ct);

    public Task<List<OutageDto>?> GetOutagesAsync(CancellationToken ct) =>
        Client.GetFromJsonAsync<List<OutageDto>>("/outages", ResponseOptions, ct);

    public Task<HttpResponseMessage> CreateWorkOrderAsync(CreateWorkOrderRequest request, CancellationToken ct) =>
        Client.PostAsJsonAsync("/work-orders", request, ct);

    public Task<HttpResponseMessage> UpdateStatusAsync(Guid id, UpdateWorkOrderStatusRequest request, CancellationToken ct) =>
        Client.PatchAsJsonAsync($"/work-orders/{id}/status", request, ct);

    // GetFromJsonAsync throws HttpRequestException on any non-success status (including
    // 404), so it can never itself signal "not found" via a null return - callers that
    // want 404-to-null passthrough (matching GridOperations' own Results.NotFound()) need
    // this explicit status check instead.
    private async Task<T?> GetOrNullAsync<T>(string requestUri, CancellationToken ct) where T : class
    {
        var response = await Client.GetAsync(requestUri, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ResponseOptions, ct);
    }
}
