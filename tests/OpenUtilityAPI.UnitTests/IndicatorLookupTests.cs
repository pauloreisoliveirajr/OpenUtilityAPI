using System.Net;
using OpenUtilityAPI.Brazil;
using OpenUtilityAPI.Infrastructure;

namespace OpenUtilityAPI.UnitTests;

public sealed class IndicatorLookupTests
{
    [Theory]
    [InlineData("SELIC-TARGET", 432, "% per year", "daily")]
    [InlineData("cdi", 12, "% per day", "daily")]
    [InlineData("ipca", 433, "monthly % change", "monthly")]
    public void CatalogDefinesUnitsAndFrequencies(string code, int series, string unit, string frequency)
    {
        var indicator = Indicators.Find(code);
        Assert.NotNull(indicator);
        Assert.Equal(series, indicator.SeriesId);
        Assert.Equal(unit, indicator.Unit);
        Assert.Equal(frequency, indicator.ObservationFrequency);
    }

    [Theory]
    [InlineData("selic-target", 432, "05/11/2026", "13.75", 13.75)]
    [InlineData("CDI", 12, "05/11/2026", "0.055131", 0.055131)]
    [InlineData("ipca", 433, "01/09/2026", "-0.12", -0.12)]
    public async Task ProviderPreservesDecimalValueAndReferenceDate(string code, int series, string date, string value, decimal expected)
    {
        using var handler = new StubHandler((request, _) =>
        {
            Assert.Equal($"https://api.bcb.gov.br/dados/serie/bcdata.sgs.{series}/dados/ultimos/1?formato=json",
                request.RequestUri!.AbsoluteUri);
            return Respond($$"""[{"data":"{{date}}","valor":"{{value}}"}]""");
        });
        using var client = CreateClient(handler);
        var observation = await new BcbIndicatorLookup(client).GetLatestAsync(code);
        Assert.NotNull(observation);
        Assert.Equal(expected, observation.Value);
        Assert.Equal(series, observation.Indicator.SeriesId);
        Assert.Equal(DateOnly.ParseExact(date, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture), observation.ReferenceDate);
        Assert.Equal(TimeSpan.Zero, observation.RetrievedAtUtc.Offset);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("""[{"data":"2026-11-05","valor":"13.75"}]""")]
    [InlineData("""[{"data":"31/02/2026","valor":"13.75"}]""")]
    [InlineData("""[{"data":"05/11/2026","valor":"13,75"}]""")]
    [InlineData("""[{"data":"05/11/2026","valor":"1e2"}]""")]
    [InlineData("""[{"data":"05/11/2026","valor":"999999999999999999999999999999999999"}]""")]
    [InlineData("""[{"data":"05/11/2026","valor":null}]""")]
    [InlineData("""[{"data":"05/11/2026","valor":"13.75"},{"data":"06/11/2026","valor":"13.75"}]""")]
    public async Task ProviderRejectsMalformedObservations(string json)
    {
        using var handler = new StubHandler((_, _) => Respond(json));
        using var client = CreateClient(handler);
        var failure = await Assert.ThrowsAsync<IndicatorLookupException>(() => new BcbIndicatorLookup(client).GetLatestAsync("selic-target"));
        Assert.False(failure.IsTimeout);
    }

    [Fact]
    public async Task MonthlyObservationRequiresFirstDayOfReferenceMonth()
    {
        using var handler = new StubHandler((_, _) => Respond("""[{"data":"15/09/2026","valor":"0.5"}]"""));
        using var client = CreateClient(handler);
        await Assert.ThrowsAsync<IndicatorLookupException>(() => new BcbIndicatorLookup(client).GetLatestAsync("ipca"));
    }

    [Fact]
    public async Task EmptySeriesHasNoAvailableObservation()
    {
        using var handler = new StubHandler((_, _) => Respond("[]"));
        using var client = CreateClient(handler);
        Assert.Null(await new BcbIndicatorLookup(client).GetLatestAsync("ipca"));
    }

    [Fact]
    public async Task UnsupportedCodesNeverReachProvider()
    {
        using var handler = new StubHandler((_, _) => throw new InvalidOperationException("Must not be called"));
        using var client = CreateClient(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => new BcbIndicatorLookup(client).GetLatestAsync("../433"));
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.NotModified)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task HttpFailuresAreProviderFailures(HttpStatusCode status)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(status)));
        using var client = CreateClient(handler);
        await Assert.ThrowsAsync<IndicatorLookupException>(() => new BcbIndicatorLookup(client).GetLatestAsync("cdi"));
    }

    [Fact]
    public async Task ProviderDistinguishesTimeoutAndCallerCancellation()
    {
        using var handler = new StubHandler((_, _) => Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true)));
        using var client = CreateClient(handler);
        var provider = new BcbIndicatorLookup(client);
        var failure = await Assert.ThrowsAsync<IndicatorLookupException>(() => provider.GetLatestAsync("cdi"));
        Assert.True(failure.IsTimeout);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetLatestAsync("cdi", cancelled.Token));
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://api.bcb.gov.br/")
    };

    private static Task<HttpResponseMessage> Respond(string json) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
