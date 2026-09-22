using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GridPulse.GridOperationsGateway.Contracts;
using Microsoft.Extensions.Options;

namespace GridPulse.GridOperationsGateway;

public sealed class CensusGeocoder(IHttpClientFactory httpClientFactory, IOptions<GridOperationsGatewayOptions> options)
{
    public async Task<GeocodeResultDto?> GeocodeAsync(int? streetNumber, string streetName, CancellationToken cancellationToken)
    {
        var address = $"{streetNumber} {streetName}, {options.Value.City}, MI {options.Value.ZipCode}";
        var requestUri = $"locations/onelineaddress?address={Uri.EscapeDataString(address)}&benchmark=Public_AR_Current&format=json";

        var httpClient = httpClientFactory.CreateClient("census-geocoder");
        var response = await httpClient.GetFromJsonAsync<CensusGeocodeResponse>(requestUri, cancellationToken);

        var coordinates = response?.Result?.AddressMatches?.FirstOrDefault()?.Coordinates;
        return coordinates is null ? null : new GeocodeResultDto { Latitude = coordinates.Y, Longitude = coordinates.X };
    }

    private sealed record CensusGeocodeResponse([property: JsonPropertyName("result")] CensusGeocodeResult? Result);

    private sealed record CensusGeocodeResult([property: JsonPropertyName("addressMatches")] List<CensusAddressMatch>? AddressMatches);

    private sealed record CensusAddressMatch([property: JsonPropertyName("coordinates")] CensusCoordinates? Coordinates);

    private sealed record CensusCoordinates([property: JsonPropertyName("x")] double X, [property: JsonPropertyName("y")] double Y);
}
