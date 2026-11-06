using System.Net;
using System.Text.Json;
using OpenUtilityAPI.Brazil;
using OpenUtilityAPI.Infrastructure;

namespace OpenUtilityAPI.UnitTests;

public sealed class LocalityLookupTests
{
    private const string MunicipalitiesJson = """
        [{"municipio-id":3550308,"municipio-nome":"São Paulo","UF-sigla":"SP"},
         {"municipio-id":3509502,"municipio-nome":"Campinas","UF-sigla":"SP"}]
        """;

    [Theory]
    [InlineData("sp", "SP")]
    [InlineData("DF", "DF")]
    public void NormalizesStateAbbreviations(string input, string expected) =>
        Assert.Equal(expected, StateCodes.Normalize(input));

    [Theory]
    [InlineData("35")]
    [InlineData("ZZ")]
    [InlineData(" SP")]
    [InlineData("../states")]
    public void RejectsUnsupportedStates(string input) =>
        Assert.Throws<ArgumentException>(() => StateCodes.Normalize(input));

    [Fact]
    public async Task ProviderMapsCompleteStatesAndOrdersByName()
    {
        var json = JsonSerializer.Serialize(StateCodes.All.Select(pair => new
        {
            id = pair.Value, sigla = pair.Key, nome = pair.Key,
            regiao = new { id = 1, nome = "Region" }
        }));
        using var handler = new StubHandler((request, _) =>
        {
            Assert.Equal("https://servicodados.ibge.gov.br/api/v1/localidades/estados?orderBy=nome", request.RequestUri!.AbsoluteUri);
            return Respond(json);
        });
        using var client = CreateClient(handler);
        var states = await new IbgeLocalityLookup(client).GetStatesAsync();
        Assert.Equal(27, states.Count);
        Assert.Equal("AC", states[0].Abbreviation);
        Assert.Equal(35, states.Single(state => state.Abbreviation == "SP").Id);
    }

    [Fact]
    public async Task ProviderMapsAndSortsMunicipalitiesForNormalizedState()
    {
        using var handler = new StubHandler((request, _) =>
        {
            Assert.Equal("https://servicodados.ibge.gov.br/api/v1/localidades/estados/SP/municipios?orderBy=nome&view=nivelado",
                request.RequestUri!.AbsoluteUri);
            return Respond(MunicipalitiesJson);
        });
        using var client = CreateClient(handler);
        var municipalities = await new IbgeLocalityLookup(client).GetMunicipalitiesAsync("sp");
        Assert.Equal(new[] { "Campinas", "São Paulo" }, municipalities.Select(item => item.Name));
        Assert.All(municipalities, item => Assert.Equal("SP", item.State));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("""[{"id":35,"sigla":"SP","nome":"São Paulo","regiao":{"id":3,"nome":"Sudeste"}}]""")]
    public async Task ProviderRejectsUnusableStateLists(string json)
    {
        using var handler = new StubHandler((_, _) => Respond(json));
        using var client = CreateClient(handler);
        var failure = await Assert.ThrowsAsync<LocalityLookupException>(() => new IbgeLocalityLookup(client).GetStatesAsync());
        Assert.False(failure.IsTimeout);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("""[{"municipio-id":5300108,"municipio-nome":"Brasília","UF-sigla":"DF"}]""")]
    [InlineData("""[{"municipio-id":5300108,"municipio-nome":"Brasília","UF-sigla":"SP"}]""")]
    [InlineData("""[{"municipio-id":3550308,"municipio-nome":" ","UF-sigla":"SP"}]""")]
    [InlineData("""[{"municipio-id":3550308,"municipio-nome":"São Paulo","UF-sigla":"SP"},{"municipio-id":3550308,"municipio-nome":"São Paulo","UF-sigla":"SP"}]""")]
    public async Task ProviderRejectsMismatchesAndDuplicateMunicipalities(string json)
    {
        using var handler = new StubHandler((_, _) => Respond(json));
        using var client = CreateClient(handler);
        await Assert.ThrowsAsync<LocalityLookupException>(() => new IbgeLocalityLookup(client).GetMunicipalitiesAsync("SP"));
    }

    [Fact]
    public async Task ProviderRejectsInvalidInputBeforeSendingRequest()
    {
        using var handler = new StubHandler((_, _) => throw new InvalidOperationException("Must not be called"));
        using var client = CreateClient(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => new IbgeLocalityLookup(client).GetMunicipalitiesAsync("ZZ"));
    }

    [Fact]
    public async Task ProviderWrapsHttpFailure()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        using var client = CreateClient(handler);
        await Assert.ThrowsAsync<LocalityLookupException>(() => new IbgeLocalityLookup(client).GetStatesAsync());
    }

    [Fact]
    public async Task ProviderDistinguishesTimeoutAndCallerCancellation()
    {
        using var handler = new StubHandler((_, _) => Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true)));
        using var client = CreateClient(handler);
        var provider = new IbgeLocalityLookup(client);
        var failure = await Assert.ThrowsAsync<LocalityLookupException>(() => provider.GetStatesAsync());
        Assert.True(failure.IsTimeout);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetStatesAsync(cancelled.Token));
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://servicodados.ibge.gov.br/")
    };

    private static Task<HttpResponseMessage> Respond(string json) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
