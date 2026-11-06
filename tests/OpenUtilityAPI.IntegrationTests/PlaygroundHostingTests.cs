using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.RegularExpressions;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class PlaygroundHostingTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Homologation", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    public async Task PlaygroundIsPublicAndSwaggerIsEnvironmentSpecific(string environment, bool swaggerEnabled)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = factory.CreateClient();
        using var page = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("text/html", page.Content.Headers.ContentType?.MediaType);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("lang=\"pt-BR\"", html);
        Assert.Contains("id=\"root\"", html);
        var assets = Regex.Matches(html, "(?:src|href)=\"(/assets/[^\"]+)\"")
            .Select(match => match.Groups[1].Value).Distinct().ToArray();
        Assert.Contains(assets, asset => asset.EndsWith(".js", StringComparison.Ordinal));
        Assert.Contains(assets, asset => asset.EndsWith(".css", StringComparison.Ordinal));
        foreach (var asset in assets)
        {
            using var response = await client.GetAsync(asset);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        foreach (var path in new[] { "/swagger/index.html", "/swagger/v1/swagger.json" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(swaggerEnabled ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
        }
        using var missing = await client.GetAsync("/api/v1/missing");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
