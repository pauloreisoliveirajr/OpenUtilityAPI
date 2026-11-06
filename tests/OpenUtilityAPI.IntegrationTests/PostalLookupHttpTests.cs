using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenUtilityAPI.Api;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class PostalLookupHttpTests
{
    [Theory]
    [InlineData("01001000", 200)]
    [InlineData("01001-000", 200)]
    [InlineData("99999999", 404)]
    [InlineData("invalid", 400)]
    [InlineData("00000001", 502)]
    [InlineData("00000002", 504)]
    public async Task LookupUsesDocumentedStatusCodes(string postalCode, int expectedStatus)
    {
        var provider = new FakeLookup();
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/br/addresses/" + postalCode);
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        if (expectedStatus == 200)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("01001000", body.RootElement.GetProperty("postalCode").GetString());
        }
        else Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(expectedStatus == 400 ? 0 : 1, provider.Calls);
    }

    [Fact]
    public async Task NormalizedAndNegativeLookupsAreCachedButFailuresAreRetried()
    {
        var provider = new FakeLookup();
        using var factory = CreateFactory(provider);
        using var client = factory.CreateClient();
        foreach (var postalCode in new[] { "01001000", "01001-000", "99999999", "99999999", "00000001", "00000001" })
        {
            using var response = await client.GetAsync("/api/v1/br/addresses/" + postalCode);
        }
        Assert.Equal(4, provider.Calls);
    }

    [Fact]
    public async Task ConcurrentCacheMissesCallProviderOnce()
    {
        var provider = new FakeLookup();
        using var cache = new PostalAddressCache();
        var results = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => cache.FindAsync("01001000", provider, CancellationToken.None)));
        Assert.All(results, address => Assert.NotNull(address));
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task SwaggerDocumentsLookupSuccessAndProviderErrors()
    {
        using var factory = CreateFactory(new FakeLookup());
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var responses = document.RootElement.GetProperty("paths").GetProperty("/api/v1/br/addresses/{postalCode}")
            .GetProperty("get").GetProperty("responses");
        foreach (var code in new[] { "200", "400", "404", "429", "500", "502", "504" })
            Assert.True(responses.TryGetProperty(code, out _));
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeLookup provider) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPostalAddressLookup>();
            services.AddSingleton<IPostalAddressLookup>(provider);
        }));

    private sealed class FakeLookup : IPostalAddressLookup
    {
        public int Calls;
        public async Task<PostalAddress?> FindAsync(string postalCode, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return postalCode switch
            {
                "99999999" => null,
                "00000001" => throw new PostalLookupException("Provider failure"),
                "00000002" => throw new PostalLookupException("Provider timeout", true),
                _ => new(postalCode, "Praça da Sé", "", "Sé", "São Paulo", "SP", "3550308")
            };
        }
    }
}
