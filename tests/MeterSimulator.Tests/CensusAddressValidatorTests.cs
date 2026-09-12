using System.Net;

namespace GridPulse.MeterSimulator.Tests;

public sealed class CensusAddressValidatorTests
{
    private static MeterSimulatorOptions Options() => new()
    {
        StreetName = "W Michigan Ave",
        StartingAddress = 100,
        BuildingsPerSide = 14,
        City = "Lansing",
        ZipCode = "48933"
    };

    [Fact]
    public async Task IsRealAddressAsync_MatchFound_ReturnsTrue()
    {
        const string responseJson = """{"result":{"addressMatches":[{"matchedAddress":"100 W MICHIGAN AVE, LANSING, MI, 48933"}]}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseJson);
        var validator = new CensusAddressValidator(new FakeHttpClientFactory(handler));

        var isReal = await validator.IsRealAddressAsync(Options(), CancellationToken.None);

        Assert.True(isReal);
    }

    [Fact]
    public async Task IsRealAddressAsync_NoMatchFound_ReturnsFalse()
    {
        const string responseJson = """{"result":{"addressMatches":[]}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseJson);
        var validator = new CensusAddressValidator(new FakeHttpClientFactory(handler));

        var isReal = await validator.IsRealAddressAsync(Options(), CancellationToken.None);

        Assert.False(isReal);
    }

    [Fact]
    public async Task IsRealAddressAsync_ApiReturnsError_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "");
        var validator = new CensusAddressValidator(new FakeHttpClientFactory(handler));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => validator.IsRealAddressAsync(Options(), CancellationToken.None));
    }
}
