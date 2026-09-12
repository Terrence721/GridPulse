using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GridPulse.MeterSimulator;

public sealed class CensusAddressValidator(IHttpClientFactory httpClientFactory)
{
    public async Task<bool> IsRealAddressAsync(MeterSimulatorOptions options, CancellationToken cancellationToken)
    {
        var address = $"{options.StartingAddress} {options.StreetName}, {options.City}, MI {options.ZipCode}";
        var requestUri = $"locations/onelineaddress?address={Uri.EscapeDataString(address)}&benchmark=Public_AR_Current&format=json";

        var httpClient = httpClientFactory.CreateClient("census-geocoder");
        var response = await httpClient.GetFromJsonAsync<CensusGeocodeResponse>(requestUri, cancellationToken);

        return response?.Result?.AddressMatches is { Count: > 0 };
    }

    private sealed record CensusGeocodeResponse([property: JsonPropertyName("result")] CensusGeocodeResult? Result);

    private sealed record CensusGeocodeResult([property: JsonPropertyName("addressMatches")] List<CensusAddressMatch>? AddressMatches);

    private sealed record CensusAddressMatch([property: JsonPropertyName("matchedAddress")] string? MatchedAddress);
}
