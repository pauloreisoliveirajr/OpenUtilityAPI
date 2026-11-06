using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class HostingPolicyTests
{
    [Fact]
    public async Task RateLimitReturnsProblemAndRetryAfter()
    {
        using var factory = CreateFactory(new() { ["RateLimit:RequestsPerMinute"] = "1" });
        using var client = factory.CreateClient();
        using var first = await client.GetAsync("/health");
        using var rejected = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        using var problem = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        Assert.Equal(429, problem.RootElement.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("https://client.example", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task CorsAllowsOnlyConfiguredOrigins(string origin, bool allowed)
    {
        using var factory = CreateFactory(new() { ["Cors:Origins:0"] = "https://client.example" });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/text/normalize");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");
        using var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
            Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task ProductionDoesNotExposeSwagger(string path)
    {
        using var factory = CreateFactory(new(), "Production");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProductionExceptionsReturnGenericProblem()
    {
        using var factory = CreateFactory(new(), "Production").WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddControllers().AddApplicationPart(typeof(FailureProbeController).Assembly)));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/test/failure");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(500, problem.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("Sensitive failure probe", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    private static WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> configuration,
        string environment = "Development") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration));
        });
}

// Registered only by the exception-handling test.
[ApiController]
public sealed class FailureProbeController : ControllerBase
{
    [HttpGet("/test/failure")]
    public IActionResult Get() => throw new InvalidOperationException("Sensitive failure probe");
}
