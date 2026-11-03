using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class UtilityHttpTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("converters/units", """{"value":32,"fromUnit":"F","toUnit":"C"}""", "\"result\":0")]
    [InlineData("converters/number-to-words", """{"number":100,"culture":"pt-BR"}""", "cem")]
    [InlineData("datetime/difference", """{"start":"2024-02-28","end":"2024-03-01"}""", "\"days\":2")]
    [InlineData("datetime/add-business-days", """{"start":"2024-03-01","days":1,"holidays":["2024-03-04"]}""", "2024-03-05")]
    [InlineData("text/normalize", """{"text":" Café   com leite ","trim":true,"collapseWhitespace":true,"removeAccents":true,"letterCase":"upper"}""", "CAFE COM LEITE")]
    [InlineData("text/email/validate", """{"value":"person@example.com"}""", "\"isValid\":true")]
    [InlineData("text/email/validate", """{"value":"person..name@example.com"}""", "\"isValid\":false")]
    public async Task ReturnsExpectedResult(string route, string json, string expected)
    {
        using var client = factory.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"/api/v1/{route}", content);
        response.EnsureSuccessStatusCode();
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("datetime/difference", """{"end":"2024-03-01"}""")]
    [InlineData("datetime/add-business-days", """{"start":"9999-12-31","days":1}""")]
    [InlineData("converters/units", """{"value":1,"fromUnit":"m","toUnit":"kg"}""")]
    [InlineData("converters/number-to-words", """{"number":1000000,"culture":"en-US"}""")]
    public async Task InvalidRequestsReturnProblemDetails(string route, string json)
    {
        using var client = factory.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"/api/v1/{route}", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SwaggerListsAllMvpPostOperations()
    {
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.Equal(15, paths.EnumerateObject().Count(path =>
            path.Name.StartsWith("/api/v1/", StringComparison.Ordinal) && path.Value.TryGetProperty("post", out _)));
    }
}
