namespace GridPulse.Billing.Tests;

internal sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler)
    {
        BaseAddress = new Uri("http://usage-aggregation")
    };
}
