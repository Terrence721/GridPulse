using System.Net.Http.Json;
using System.Text.Json;

namespace GridPulse.MeterSimulator;

public sealed class AccountResolver(IHttpClientFactory httpClientFactory)
{
    // AccountCustomer's minimal API serializes responses with the ASP.NET Core Web
    // defaults (camelCase), but GetFromJsonAsync without explicit options deserializes
    // case-sensitively against these PascalCase record properties - every property
    // silently bound to its default (Guid.Empty for AccountId) with no exception.
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyDictionary<string, Guid>> ResolveAccountIdsAsync(IReadOnlyList<string> meterIds, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("account-customer");
        var accountIdsByMeterId = new Dictionary<string, Guid>();

        foreach (var meterId in meterIds)
        {
            var meter = await httpClient.GetFromJsonAsync<MeterLookupResponse>($"/meters/{Uri.EscapeDataString(meterId)}", ResponseOptions, cancellationToken);
            accountIdsByMeterId[meterId] = meter!.AccountId;
        }

        return accountIdsByMeterId;
    }

    private sealed record MeterLookupResponse(string MeterId, Guid AccountId);
}
