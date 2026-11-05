using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OpenUtilityAPI.IntegrationTests;

public sealed class SwaggerContractTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task EveryPostDocumentsItsSuccessDtoAndErrorResponses()
    {
        using var document = await ReadSwagger();
        var expectedDtos = new Dictionary<string, string>
        {
            ["br/documents/cpf/validate"] = "DocumentValidation",
            ["br/documents/cnpj/validate"] = "DocumentValidation",
            ["br/documents/pis/validate"] = "DocumentValidation",
            ["br/documents/format"] = "FormattedDocument",
            ["finance/simple-interest"] = "InterestResult",
            ["finance/compound-interest"] = "InterestResult",
            ["finance/price"] = "LoanResult",
            ["finance/sac"] = "LoanResult",
            ["math/percentage"] = "PercentageResult",
            ["converters/units"] = "UnitConversion",
            ["converters/number-to-words"] = "NumberWordsResponse",
            ["datetime/difference"] = "DateDifference",
            ["datetime/add-business-days"] = "BusinessDaysResponse",
            ["text/normalize"] = "NormalizedText",
            ["text/email/validate"] = "EmailValidation"
        };

        foreach (var (route, dto) in expectedDtos)
        {
            var responses = document.RootElement.GetProperty("paths")
                .GetProperty($"/api/v1/{route}").GetProperty("post").GetProperty("responses");
            var schema = responses.GetProperty("200").GetProperty("content")
                .GetProperty("application/json").GetProperty("schema");
            Assert.Equal($"#/components/schemas/{dto}", schema.GetProperty("$ref").GetString());
            Assert.True(document.RootElement.GetProperty("components").GetProperty("schemas").TryGetProperty(dto, out _));
            foreach (var status in new[] { "400", "429", "500" })
                Assert.True(responses.TryGetProperty(status, out _));
        }
    }

    [Fact]
    public async Task RequestSchemasDescribeValidationExamplesAndActualDefaults()
    {
        using var document = await ReadSwagger();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var finance = schemas.GetProperty("FinanceRequest");
        var properties = finance.GetProperty("properties");
        AssertRequired(finance, "principal", "ratePerPeriod", "periods");
        AssertRange(properties.GetProperty("principal"), 0, 1_000_000_000_000m);
        AssertRange(properties.GetProperty("ratePerPeriod"), 0, 0.05m);
        AssertRange(properties.GetProperty("periods"), 0, 360);
        AssertRange(properties.GetProperty("decimalPlaces"), 0, 6);
        Assert.Equal(2, properties.GetProperty("decimalPlaces").GetProperty("default").GetInt32());
        Assert.Equal(0.01m, properties.GetProperty("ratePerPeriod").GetProperty("example").GetDecimal());
        Assert.False(properties.GetProperty("ratePerPeriod").TryGetProperty("default", out _));

        var percentage = schemas.GetProperty("PercentageRequest");
        AssertRequired(percentage, "operation", "value", "operand");
        var operation = percentage.GetProperty("properties").GetProperty("operation");
        Assert.Equal("string", operation.GetProperty("type").GetString());
        Assert.False(operation.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean());
        Assert.Equal("percentOf|ratioPercent|changePercent", operation.GetProperty("pattern").GetString());
        Assert.Equal("percentOf", operation.GetProperty("example").GetString());

        AssertRequired(schemas.GetProperty("DocumentRequest"), "value");
        Assert.Equal(18, schemas.GetProperty("DocumentRequest").GetProperty("properties").GetProperty("value").GetProperty("maxLength").GetInt32());
        AssertRequired(schemas.GetProperty("FormatDocumentRequest"), "type", "value");
        Assert.Equal("cpf|cnpj", schemas.GetProperty("FormatDocumentRequest").GetProperty("properties").GetProperty("type").GetProperty("pattern").GetString());
        AssertRequired(schemas.GetProperty("UnitRequest"), "value", "fromUnit", "toUnit");
        AssertRange(schemas.GetProperty("UnitRequest").GetProperty("properties").GetProperty("decimalPlaces"), 0, 12);
        Assert.Equal(6, schemas.GetProperty("UnitRequest").GetProperty("properties").GetProperty("decimalPlaces").GetProperty("default").GetInt32());
        AssertRequired(schemas.GetProperty("NumberWordsRequest"), "number", "culture");
        AssertRange(schemas.GetProperty("NumberWordsRequest").GetProperty("properties").GetProperty("number"), -999999, 999999);
        AssertRequired(schemas.GetProperty("DateDifferenceRequest"), "start", "end");
        Assert.Equal("date", schemas.GetProperty("DateDifferenceRequest").GetProperty("properties").GetProperty("start").GetProperty("format").GetString());
        var business = schemas.GetProperty("BusinessDaysRequest");
        AssertRequired(business, "start", "days");
        AssertRange(business.GetProperty("properties").GetProperty("days"), -10000, 10000);
        Assert.Equal(366, business.GetProperty("properties").GetProperty("holidays").GetProperty("maxItems").GetInt32());
        var text = schemas.GetProperty("TextRequest");
        AssertRequired(text, "text");
        Assert.Equal(0, text.GetProperty("properties").GetProperty("text").GetProperty("minLength").GetInt32());
        Assert.Equal(10000, text.GetProperty("properties").GetProperty("text").GetProperty("maxLength").GetInt32());
        Assert.Equal("preserve", text.GetProperty("properties").GetProperty("letterCase").GetProperty("default").GetString());
        Assert.False(text.GetProperty("properties").GetProperty("trim").GetProperty("default").GetBoolean());
        AssertRequired(schemas.GetProperty("EmailRequest"), "value");
        Assert.Equal(254, schemas.GetProperty("EmailRequest").GetProperty("properties").GetProperty("value").GetProperty("maxLength").GetInt32());
    }

    [Theory]
    [InlineData("finance/compound-interest", """{"principal":1000,"ratePerPeriod":0.01,"periods":12}""", HttpStatusCode.OK)]
    [InlineData("finance/compound-interest", """{"principal":1000,"ratePerPeriod":0.06,"periods":12}""", HttpStatusCode.BadRequest)]
    [InlineData("finance/compound-interest", """{"ratePerPeriod":0.01,"periods":12}""", HttpStatusCode.BadRequest)]
    [InlineData("math/percentage", """{"value":200,"operand":15}""", HttpStatusCode.BadRequest)]
    [InlineData("math/percentage", """{"operation":null,"value":200,"operand":15}""", HttpStatusCode.BadRequest)]
    [InlineData("math/percentage", """{"operation":"invalid","value":200,"operand":15}""", HttpStatusCode.BadRequest)]
    [InlineData("text/normalize", """{"text":""}""", HttpStatusCode.OK)]
    [InlineData("text/normalize", """{"text":null}""", HttpStatusCode.BadRequest)]
    [InlineData("text/normalize", """{"text":"test","letterCase":null}""", HttpStatusCode.BadRequest)]
    public async Task SchemaMetadataDoesNotChangeRuntimeValidation(string route, string json, HttpStatusCode expected)
    {
        using var client = factory.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"/api/v1/{route}", content);
        Assert.Equal(expected, response.StatusCode);
    }

    private async Task<JsonDocument> ReadSwagger()
    {
        using var client = factory.CreateClient();
        return JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
    }

    private static void AssertRequired(JsonElement schema, params string[] expected) =>
        Assert.Equal(expected.Order(), schema.GetProperty("required").EnumerateArray().Select(item => item.GetString()).Order());

    private static void AssertRange(JsonElement schema, decimal minimum, decimal maximum)
    {
        Assert.Equal(minimum, schema.GetProperty("minimum").GetDecimal());
        Assert.Equal(maximum, schema.GetProperty("maximum").GetDecimal());
    }
}
