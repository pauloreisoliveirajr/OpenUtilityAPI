using Microsoft.AspNetCore.Mvc.Testing;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class CalculationHttpTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("finance/simple-interest", "{\"principal\":1000,\"ratePerPeriod\":0.01,\"periods\":12}", "1120")]
    [InlineData("finance/compound-interest", "{\"principal\":1000,\"ratePerPeriod\":0.01,\"periods\":12}", "1126.83")]
    [InlineData("finance/price", "{\"principal\":1000,\"ratePerPeriod\":0,\"periods\":12}", "\"balance\":0")]
    [InlineData("finance/sac", "{\"principal\":1000,\"ratePerPeriod\":0,\"periods\":12}", "\"balance\":0")]
    [InlineData("math/percentage", "{\"operation\":\"percentOf\",\"value\":200,\"operand\":15}", "\"result\":30")]
    public async Task Calculates(string endpoint, string json, string expected)
    {
        using var client = factory.CreateClient();
        using var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"/api/v1/{endpoint}", body);
        response.EnsureSuccessStatusCode();
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
    }
}
