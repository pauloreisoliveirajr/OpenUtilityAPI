using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenUtilityAPI.Api;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class BankHttpTests
{
    [Theory]
    [InlineData("1", 200)]
    [InlineData("001", 200)]
    [InlineData("999", 404)]
    [InlineData("000", 400)]
    [InlineData("abc", 400)]
    [InlineData("1000", 400)]
    public async Task FindsBankByCompeWithDocumentedResponses(string code, int expectedStatus)
    {
        var provider = new FakeLookup();
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/br/banks/" + code);
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        if (expectedStatus == 200)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("00000000", json.RootElement.GetProperty("ispb").GetString());
            Assert.Equal(1, json.RootElement.GetProperty("code").GetInt32());
        }
        else Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(expectedStatus == 400 ? 0 : 1, provider.Calls);
    }

    [Fact]
    public async Task ListingAndCodeLookupsShareOneDirectorySnapshot()
    {
        var provider = new FakeLookup();
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        using var directory = await client.GetAsync("/api/v1/br/banks");
        Assert.Equal(HttpStatusCode.OK, directory.StatusCode);
        using var json = JsonDocument.Parse(await directory.Content.ReadAsStringAsync());
        Assert.Equal(2, json.RootElement.GetArrayLength());
        Assert.Equal(JsonValueKind.Null, json.RootElement[1].GetProperty("code").ValueKind);
        foreach (var code in new[] { "1", "001", "999", "999" })
        {
            using var response = await client.GetAsync("/api/v1/br/banks/" + code);
        }
        Assert.Equal(1, provider.Calls);
    }

    [Theory]
    [InlineData(false, 502)]
    [InlineData(true, 504)]
    public async Task ProviderFailuresAreGenericAndNotCached(bool timeout, int status)
    {
        var provider = new FakeLookup { Failure = new BankLookupException("Private provider details", timeout) };
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        foreach (var path in new[] { "/api/v1/br/banks", "/api/v1/br/banks/001" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain("Private provider details", await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task ConcurrentMissesCallProviderOnceAndCancelledReadsHonorCancellation()
    {
        var provider = new FakeLookup();
        using var cache = new BankDirectoryCache();
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => cache.GetBanksAsync(provider, CancellationToken.None)));
        Assert.Equal(1, provider.Calls);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetBanksAsync(provider, cancelled.Token));
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task CacheIsReadOnlyAndDetachedFromProviderList()
    {
        var provider = new FakeLookup();
        using var cache = new BankDirectoryCache();
        var snapshot = await cache.GetBanksAsync(provider, CancellationToken.None);
        provider.Banks.Clear();
        Assert.Equal(2, snapshot.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<Bank>)snapshot).Clear());
        Assert.Equal(2, (await cache.GetBanksAsync(provider, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task SwaggerDescribesDirectoryAndCodeLookup()
    {
        using var factory = CreateFactory(new FakeLookup());
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        foreach (var path in new[] { "/api/v1/br/banks", "/api/v1/br/banks/{code}" })
        {
            var responses = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get").GetProperty("responses");
            foreach (var status in new[] { "200", "400", "429", "500", "502", "504" })
                Assert.True(responses.TryGetProperty(status, out _), $"Missing {status} response for {path}.");
            if (path.Contains("{code}", StringComparison.Ordinal))
                Assert.True(responses.TryGetProperty("404", out _));
            Assert.True(responses.GetProperty("200").TryGetProperty("content", out _));
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeLookup provider) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBankLookup>();
            services.AddSingleton<IBankLookup>(provider);
        }));

    private sealed class FakeLookup : IBankLookup
    {
        public int Calls;
        public BankLookupException? Failure;
        public List<Bank> Banks { get; } = [new("00000000", 1, "Bank", "Example Bank"), new("00038166", null, "Institution", "Example Institution")];

        public async Task<IReadOnlyList<Bank>> GetBanksAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            return Banks;
        }
    }
}
