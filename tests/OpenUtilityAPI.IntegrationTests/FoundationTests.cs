using Microsoft.AspNetCore.Mvc.Testing;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class FoundationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/health", "Healthy")]
    [InlineData("/swagger/index.html", "Swagger UI")]
    [InlineData("/swagger/v1/swagger.json", "openapi")]
    public async Task DevelopmentRoutesRespond(string path, string expected)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path, TestContextCancellation);
        response.EnsureSuccessStatusCode();
        Assert.Contains(expected, await response.Content.ReadAsStringAsync(TestContextCancellation));
    }

    private static CancellationToken TestContextCancellation => CancellationToken.None;
}
