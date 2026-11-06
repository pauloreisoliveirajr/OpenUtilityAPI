using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
        Assert.Contains("lang=\"pt-BR\"", await page.Content.ReadAsStringAsync());
        foreach (var asset in new[] { "/playground.js", "/tools.js", "/language.js", "/playground.css" })
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
