using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenUtilityAPI.Api;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class LocalityHttpTests
{
    [Theory]
    [InlineData("states")]
    [InlineData("states/sp/municipalities")]
    [InlineData("states/DF/municipalities")]
    public async Task ListsLocalitiesAndCachesByNormalizedState(string path)
    {
        var provider = new FakeLookup();
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        using var first = await client.GetAsync("/api/v1/br/localities/" + path);
        using var second = await client.GetAsync("/api/v1/br/localities/" + path.Replace("/sp/", "/SP/", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using var json = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.NotEmpty(json.RootElement.EnumerateArray());
        Assert.Equal(1, provider.Calls);
    }

    [Theory]
    [InlineData("ZZ")]
    [InlineData("35")]
    public async Task InvalidStateReturns400WithoutProviderCall(string state)
    {
        var provider = new FakeLookup();
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/api/v1/br/localities/states/{state}/municipalities");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, provider.Calls);
    }

    [Theory]
    [InlineData(false, 502)]
    [InlineData(true, 504)]
    public async Task ProviderFailuresAreDocumentedAndNotCached(bool timeout, int status)
    {
        var provider = new FakeLookup { Failure = new LocalityLookupException("Private provider details", timeout) };
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        foreach (var path in new[] { "states", "states/SP/municipalities", "states" })
        {
            using var response = await client.GetAsync("/api/v1/br/localities/" + path);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain("Private provider details", await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(3, provider.Calls);
    }

    [Fact]
    public async Task CacheCollapsesConcurrentRequestsAndKeepsStateKeysSeparate()
    {
        var provider = new FakeLookup();
        using var cache = new LocalityCache();
        await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => cache.GetMunicipalitiesAsync("sp", provider, CancellationToken.None)));
        await cache.GetMunicipalitiesAsync("DF", provider, CancellationToken.None);
        await cache.GetStatesAsync(provider, CancellationToken.None);
        Assert.Equal(3, provider.Calls);
    }

    [Fact]
    public async Task CancelledLookupIsNotCachedAndCachedReadsHonorCancellation()
    {
        var provider = new FakeLookup();
        using var cache = new LocalityCache();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetStatesAsync(provider, cancelled.Token));
        Assert.Equal(0, provider.Calls);
        await cache.GetStatesAsync(provider, CancellationToken.None);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetStatesAsync(provider, cancelled.Token));
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task SwaggerIncludesTypedSuccessAndErrorResponses()
    {
        using var factory = CreateFactory(new FakeLookup());
        using var client = factory.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        foreach (var path in new[] { "/api/v1/br/localities/states", "/api/v1/br/localities/states/{state}/municipalities" })
        {
            var responses = json.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get").GetProperty("responses");
            foreach (var status in new[] { "200", "400", "429", "500", "502", "504" })
                Assert.True(responses.TryGetProperty(status, out _), $"Missing {status} response for {path}.");
            Assert.Equal("array", responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
                .GetProperty("schema").GetProperty("type").GetString());
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeLookup provider) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILocalityLookup>();
            services.AddSingleton<ILocalityLookup>(provider);
        }));

    private sealed class FakeLookup : ILocalityLookup
    {
        public int Calls;
        public LocalityLookupException? Failure;

        public async Task<IReadOnlyList<BrazilianState>> GetStatesAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            return [new(35, "SP", "São Paulo", 3, "Sudeste")];
        }

        public async Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(string state, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            return state == "DF" ? [new(5300108, "Brasília", state)] : [new(3550308, "São Paulo", state)];
        }
    }
}
