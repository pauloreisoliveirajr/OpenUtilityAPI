using System.Net;
using OpenUtilityAPI.Brazil;
using OpenUtilityAPI.Infrastructure;

namespace OpenUtilityAPI.UnitTests;

public sealed class PostalLookupTests
{
    private const string AddressJson = """
        {"cep":"01001-000","logradouro":"Praça da Sé","complemento":"","bairro":"Sé","localidade":"São Paulo","uf":"SP","ibge":"3550308"}
        """;

    [Theory]
    [InlineData("01001-000", "01001000")]
    [InlineData("01001000", "01001000")]
    public void PostalCodesPreserveLeadingZeros(string input, string expected) =>
        Assert.Equal(expected, PostalCodes.Normalize(input));

    [Theory]
    [InlineData("")]
    [InlineData("01001 000")]
    [InlineData("0100100A")]
    [InlineData("０１００１０００")]
    [InlineData("../health")]
    public void PostalCodesRejectInvalidInputs(string input) =>
        Assert.Throws<ArgumentException>(() => PostalCodes.Normalize(input));

    [Fact]
    public async Task ProviderMapsAddressAndUsesFixedRelativePath()
    {
        using var handler = new StubHandler((request, _) =>
        {
            Assert.Equal("https://viacep.com.br/ws/01001000/json/", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(AddressJson) });
        });
        using var client = new HttpClient(handler) { BaseAddress = new("https://viacep.com.br/") };
        var address = await new ViaCepAddressLookup(client).FindAsync("01001-000");
        Assert.Equal("01001000", address!.PostalCode);
        Assert.Equal("São Paulo", address.City);
        Assert.Equal("SP", address.State);
    }

    [Theory]
    [InlineData("""{"erro":true}""")]
    [InlineData("""{"erro":"true"}""")]
    public async Task ProviderMapsNotFound(string json)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new("https://viacep.com.br/") };
        Assert.Null(await new ViaCepAddressLookup(client).FindAsync("01001000"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"cep":"02002000","localidade":"São Paulo","uf":"SP","ibge":"3550308"}""")]
    public async Task ProviderRejectsMalformedOrMismatchedResponses(string json)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new("https://viacep.com.br/") };
        var failure = await Assert.ThrowsAsync<PostalLookupException>(() => new ViaCepAddressLookup(client).FindAsync("01001000"));
        Assert.False(failure.IsTimeout);
    }

    [Fact]
    public async Task ProviderMapsHttpFailure()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        using var client = new HttpClient(handler) { BaseAddress = new("https://viacep.com.br/") };
        await Assert.ThrowsAsync<PostalLookupException>(() => new ViaCepAddressLookup(client).FindAsync("01001000"));
    }

    [Fact]
    public async Task ProviderDistinguishesTimeoutFromCallerCancellation()
    {
        using var handler = new StubHandler((_, token) => Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true)));
        using var client = new HttpClient(handler) { BaseAddress = new("https://viacep.com.br/") };
        var provider = new ViaCepAddressLookup(client);
        var timeout = await Assert.ThrowsAsync<PostalLookupException>(() => provider.FindAsync("01001000"));
        Assert.True(timeout.IsTimeout);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.FindAsync("01001000", cancelled.Token));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
