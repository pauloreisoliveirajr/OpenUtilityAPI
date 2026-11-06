using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Infrastructure;

public sealed class IbgeLocalityLookup(HttpClient client) : ILocalityLookup
{
    private static readonly StringComparer NameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), false);

    public async Task<IReadOnlyList<BrazilianState>> GetStatesAsync(CancellationToken cancellationToken = default)
    {
        var states = await ReadAsync<StateResponse>("api/v1/localidades/estados?orderBy=nome", cancellationToken);
        if (states.Count != StateCodes.All.Count ||
            states.Any(state => state is null || string.IsNullOrWhiteSpace(state.Name) ||
                state.Abbreviation is null || !StateCodes.All.TryGetValue(state.Abbreviation, out var id) ||
                state.Id != id || state.Region is null || state.Region.Id is < 1 or > 5 ||
                string.IsNullOrWhiteSpace(state.Region.Name)) ||
            states.Select(state => state.Id).Distinct().Count() != states.Count)
            throw new LocalityLookupException("The locality provider returned an incomplete state list.");

        return Array.AsReadOnly(states.Select(state => new BrazilianState(state.Id, state.Abbreviation!,
                state.Name!, state.Region!.Id, state.Region.Name!))
            .OrderBy(state => state.Name, NameComparer).ThenBy(state => state.Id).ToArray());
    }

    public async Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(string state,
        CancellationToken cancellationToken = default)
    {
        var normalized = StateCodes.Normalize(state);
        var municipalities = await ReadAsync<MunicipalityResponse>(
            $"api/v1/localidades/estados/{normalized}/municipios?orderBy=nome&view=nivelado", cancellationToken);
        if (municipalities.Count is < 1 or > 1000 ||
            municipalities.Any(item => item is null || string.IsNullOrWhiteSpace(item.Name) ||
                item.State != normalized || item.Id is < 1000000 or > 9999999 ||
                item.Id / 100000 != StateCodes.All[normalized]) ||
            municipalities.Select(item => item.Id).Distinct().Count() != municipalities.Count)
            throw new LocalityLookupException("The locality provider returned an unusable municipality list.");

        return Array.AsReadOnly(municipalities.Select(item => new Municipality(item.Id, item.Name!, normalized))
            .OrderBy(item => item.Name, NameComparer).ThenBy(item => item.Id).ToArray());
    }

    private async Task<List<T>> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<T>>(json)
                ?? throw new JsonException("Missing provider response.");
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new LocalityLookupException("The locality provider timed out.", true, exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            throw new LocalityLookupException("The locality provider returned an unusable response.", innerException: exception);
        }
    }

    private sealed record StateResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("sigla")] string? Abbreviation,
        [property: JsonPropertyName("nome")] string? Name,
        [property: JsonPropertyName("regiao")] RegionResponse? Region);

    private sealed record RegionResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("nome")] string? Name);

    private sealed record MunicipalityResponse(
        [property: JsonPropertyName("municipio-id")] int Id,
        [property: JsonPropertyName("municipio-nome")] string? Name,
        [property: JsonPropertyName("UF-sigla")] string? State);
}
