using System.Net;
using OpenUtilityAPI.Brazil;
using OpenUtilityAPI.Infrastructure;

namespace OpenUtilityAPI.UnitTests;

public sealed class BankLookupTests
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("001", 1)]
    [InlineData("999", 999)]
    public void ParsesCompeCodes(string input, int expected) => Assert.Equal(expected, BankCodes.Parse(input));

    [Theory]
    [InlineData("")]
    [InlineData("000")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1000")]
    [InlineData(" 1")]
    [InlineData("１")]
    [InlineData("abc")]
    public void RejectsInvalidCodes(string input) => Assert.Throws<ArgumentException>(() => BankCodes.Parse(input));

    [Fact]
    public async Task ProviderPreservesIspbSortsAndNormalizesMissingCodes()
    {
        const string json = """
            [{"ispb":"00000000","name":"Beta","code":1,"fullName":"Beta Bank"},
             {"ispb":"00038166","name":"Alpha","code":null,"fullName":"Alpha Institution"},
             {"ispb":"28719664","name":"Gamma","code":0,"fullName":"Gamma Institution"},
             {"ispb":"54641030","name":"Delta","code":0,"fullName":"Delta Institution"}]
            """;
        using var handler = new StubHandler((request, _) =>
        {
            Assert.Equal("https://brasilapi.com.br/api/banks/v1", request.RequestUri!.AbsoluteUri);
            return Respond(json);
        });
        using var client = CreateClient(handler);
        var banks = await new BrasilApiBankLookup(client).GetBanksAsync();
        Assert.Equal(new[] { "Alpha", "Beta", "Delta", "Gamma" }, banks.Select(bank => bank.Name));
        Assert.Equal("00000000", banks[1].Ispb);
        Assert.Equal(1, banks[1].Code);
        Assert.Null(banks[0].Code);
        Assert.Null(banks[2].Code);
        Assert.Null(banks[3].Code);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("""[{"ispb":"123","name":"Bank","code":1,"fullName":"Bank"}]""")]
    [InlineData("""[{"ispb":"00000000","name":"","code":1,"fullName":"Bank"}]""")]
    [InlineData("""[{"ispb":"00000000","name":"Bank","code":1000,"fullName":"Bank"}]""")]
    [InlineData("""[{"ispb":"00000000","name":"Bank","code":-1,"fullName":"Bank"}]""")]
    [InlineData("""[{"ispb":"00000000","name":"Bank","code":1,"fullName":"Bank"},{"ispb":"00000000","name":"Other","code":2,"fullName":"Other Bank"}]""")]
    [InlineData("""[{"ispb":"00000000","name":"Bank","code":1,"fullName":"Bank"},{"ispb":"00000001","name":"Other","code":1,"fullName":"Other Bank"}]""")]
    public async Task ProviderRejectsInvalidOrAmbiguousRecords(string json)
    {
        using var handler = new StubHandler((_, _) => Respond(json));
        using var client = CreateClient(handler);
        var failure = await Assert.ThrowsAsync<BankLookupException>(() => new BrasilApiBankLookup(client).GetBanksAsync());
        Assert.False(failure.IsTimeout);
    }

    [Fact]
    public async Task ProviderWrapsHttpFailure()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        using var client = CreateClient(handler);
        await Assert.ThrowsAsync<BankLookupException>(() => new BrasilApiBankLookup(client).GetBanksAsync());
    }

    [Fact]
    public async Task ProviderDistinguishesTimeoutAndCallerCancellation()
    {
        using var handler = new StubHandler((_, _) => Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true)));
        using var client = CreateClient(handler);
        var provider = new BrasilApiBankLookup(client);
        var failure = await Assert.ThrowsAsync<BankLookupException>(() => provider.GetBanksAsync());
        Assert.True(failure.IsTimeout);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetBanksAsync(cancelled.Token));
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://brasilapi.com.br/")
    };

    private static Task<HttpResponseMessage> Respond(string json) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
