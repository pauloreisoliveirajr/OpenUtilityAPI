using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenUtilityAPI.Api;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class IndicatorHttpTests
{
    [Fact]
    public async Task CatalogDoesNotDependOnProviderAvailability()
    {
        var provider = new FakeLookup { Failure = new IndicatorLookupException("Unavailable") };
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/br/indicators");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, json.RootElement.GetArrayLength());
        Assert.Equal("% per year", json.RootElement[0].GetProperty("unit").GetString());
        Assert.Equal(0, provider.Calls);
    }

    [Theory]
    [InlineData("selic-target")]
    [InlineData("CDI")]
    [InlineData("ipca")]
    public async Task LatestResponseIncludesSourceUnitAndDates(string code)
    {
        var provider = new FakeLookup();
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/br/indicators/" + code);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var indicator = json.RootElement.GetProperty("indicator");
        Assert.Equal(code.ToLowerInvariant(), indicator.GetProperty("code").GetString());
        Assert.Equal("BCB SGS", indicator.GetProperty("source").GetString());
        Assert.True(indicator.TryGetProperty("unit", out _));
        Assert.True(indicator.TryGetProperty("sourceUrl", out _));
        Assert.Equal("2026-09-01", json.RootElement.GetProperty("referenceDate").GetString());
        Assert.True(json.RootElement.TryGetProperty("retrievedAtUtc", out _));
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task UnsupportedIndicatorReturns404WithoutProviderCall()
    {
        var provider = new FakeLookup();
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/br/indicators/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, provider.Calls);
    }

    [Theory]
    [InlineData(false, 502)]
    [InlineData(true, 504)]
    public async Task ProviderFailuresAreGenericAndNotCached(bool timeout, int status)
    {
        var provider = new FakeLookup { Failure = new IndicatorLookupException("Private failure details", timeout) };
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        for (var i = 0; i < 2; i++)
        {
            using var response = await client.GetAsync("/api/v1/br/indicators/ipca");
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain("Private failure details", await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task MissingObservationIs404AndCachedBriefly()
    {
        var provider = new FakeLookup { Missing = true };
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        foreach (var code in new[] { "ipca", "IPCA" })
        {
            using var response = await client.GetAsync("/api/v1/br/indicators/" + code);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task CacheCollapsesConcurrentMissesKeepsKeysSeparateAndHonorsCancellation()
    {
        var provider = new FakeLookup();
        using var cache = new IndicatorCache();
        var observations = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => cache.GetLatestAsync("CDI", provider, CancellationToken.None)));
        Assert.All(observations, item => Assert.Equal(observations[0], item));
        await cache.GetLatestAsync("ipca", provider, CancellationToken.None);
        Assert.Equal(2, provider.Calls);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetLatestAsync("cdi", provider, cancelled.Token));
    }

    [Fact]
    public async Task SwaggerDescribesCatalogAndLatestObservation()
    {
        using var factory = CreateFactory(new FakeLookup());
        using var client = factory.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = json.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/v1/br/indicators").GetProperty("get")
            .GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
        var responses = paths.GetProperty("/api/v1/br/indicators/{code}").GetProperty("get").GetProperty("responses");
        foreach (var status in new[] { "200", "400", "404", "429", "500", "502", "504" })
            Assert.True(responses.TryGetProperty(status, out _));
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeLookup provider) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIndicatorLookup>();
            services.AddSingleton<IIndicatorLookup>(provider);
        }));

    private sealed class FakeLookup : IIndicatorLookup
    {
        public int Calls;
        public IndicatorLookupException? Failure;
        public bool Missing;

        public async Task<IndicatorObservation?> GetLatestAsync(string code, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            return Missing ? null : new(Indicators.Find(code)!, -0.12m, new DateOnly(2026, 9, 1),
                new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));
        }
    }
}
