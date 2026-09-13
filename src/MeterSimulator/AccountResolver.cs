using System.Net.Http.Json;

namespace GridPulse.MeterSimulator;

public sealed class AccountResolver(IHttpClientFactory httpClientFactory)
{
    public async Task<IReadOnlyDictionary<string, Guid>> ResolveAccountIdsAsync(IReadOnlyList<string> meterIds, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("account-customer");
        var accountIdsByMeterId = new Dictionary<string, Guid>();

        foreach (var meterId in meterIds)
        {
            var meter = await httpClient.GetFromJsonAsync<MeterLookupResponse>($"/meters/{Uri.EscapeDataString(meterId)}", cancellationToken);
            accountIdsByMeterId[meterId] = meter!.AccountId;
        }

        return accountIdsByMeterId;
    }

    private sealed record MeterLookupResponse(string MeterId, Guid AccountId);
}
