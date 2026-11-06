using System.Text.Json;
using System.Text.Json.Serialization;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Infrastructure;

public sealed class ViaCepAddressLookup(HttpClient client) : IPostalAddressLookup
{
    public async Task<PostalAddress?> FindAsync(string postalCode, CancellationToken cancellationToken = default)
    {
        var normalized = PostalCodes.Normalize(postalCode);
        try
        {
            using var response = await client.GetAsync($"ws/{normalized}/json/", cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var address = JsonSerializer.Deserialize<ViaCepResponse>(json)
                ?? throw new JsonException("Missing provider response.");

            if (address.Error.ValueKind == JsonValueKind.True ||
                address.Error.ValueKind == JsonValueKind.String && address.Error.GetString() == "true")
                return null;

            if (address.PostalCode is null || PostalCodes.Normalize(address.PostalCode) != normalized ||
                string.IsNullOrWhiteSpace(address.City) || address.State is not { Length: 2 } ||
                address.State.Any(character => character is < 'A' or > 'Z') ||
                address.IbgeCode is not { Length: 7 } ||
                address.IbgeCode.Any(character => character is < '0' or > '9'))
                throw new JsonException("Incomplete provider response.");

            return new(normalized, address.Street ?? "", address.Complement ?? "",
                address.Neighborhood ?? "", address.City, address.State, address.IbgeCode);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PostalLookupException("The postal address provider timed out.", true, exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or ArgumentException)
        {
            throw new PostalLookupException("The postal address provider returned an unusable response.", innerException: exception);
        }
    }

    private sealed record ViaCepResponse(
        [property: JsonPropertyName("cep")] string? PostalCode,
        [property: JsonPropertyName("logradouro")] string? Street,
        [property: JsonPropertyName("complemento")] string? Complement,
        [property: JsonPropertyName("bairro")] string? Neighborhood,
        [property: JsonPropertyName("localidade")] string? City,
        [property: JsonPropertyName("uf")] string? State,
        [property: JsonPropertyName("ibge")] string? IbgeCode,
        [property: JsonPropertyName("erro")] JsonElement Error);
}
