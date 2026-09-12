namespace GridPulse.MeterSimulator.Tests;

internal sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler)
    {
        BaseAddress = new Uri("https://geocoding.geo.census.gov/geocoder/")
    };
}
