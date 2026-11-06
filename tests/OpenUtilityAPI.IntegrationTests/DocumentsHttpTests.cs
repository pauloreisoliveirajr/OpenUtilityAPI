using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class DocumentsHttpTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("cpf/validate", "52998224725", HttpStatusCode.OK, "\"isValid\":true")]
    [InlineData("cnpj/validate", "12ABC34501DE36", HttpStatusCode.OK, "\"isValid\":false")]
    [InlineData("pis/validate", "12044558957", HttpStatusCode.OK, "\"isValid\":true")]
    [InlineData("format", "12abc34501de35", HttpStatusCode.OK, "12.ABC.345/01DE-35")]
    [InlineData("cpf/validate", "malformed", HttpStatusCode.BadRequest, "\"errors\"")]
    public async Task PostsDocument(string endpoint, string value, HttpStatusCode status, string expected)
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync($"/api/v1/br/documents/{endpoint}", new { value, type = "cnpj" });
        Assert.Equal(status, response.StatusCode);
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
    }
}

