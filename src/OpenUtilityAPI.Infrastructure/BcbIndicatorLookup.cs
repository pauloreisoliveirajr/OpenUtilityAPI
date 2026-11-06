using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Infrastructure;

public sealed class BcbIndicatorLookup(HttpClient client) : IIndicatorLookup
{
    public async Task<IndicatorObservation?> GetLatestAsync(string code, CancellationToken cancellationToken = default)
    {
        var indicator = Indicators.Find(code)
            ?? throw new ArgumentException("Select an indicator from the supported catalog.", nameof(code));

        try
        {
            using var response = await client.GetAsync(
                $"dados/serie/bcdata.sgs.{indicator.SeriesId}/dados/ultimos/1?formato=json", cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var observations = JsonSerializer.Deserialize<List<ObservationResponse>>(json)
                ?? throw new JsonException("Missing indicator response.");

            if (observations.Count == 0)
                return null;

            if (observations.Count != 1 || observations[0] is not { } observation ||
                !DateOnly.TryParseExact(observation.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date) ||
                indicator.ObservationFrequency == "monthly" && date.Day != 1 ||
                !decimal.TryParse(observation.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var value))
                throw new JsonException("The indicator provider returned an unusable observation.");

            return new(indicator, value, date, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IndicatorLookupException("The indicator provider timed out.", true, exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            throw new IndicatorLookupException("The indicator provider returned an unusable response.", innerException: exception);
        }
    }

    private sealed record ObservationResponse(
        [property: JsonPropertyName("data")] string? Date,
        [property: JsonPropertyName("valor")] string? Value);
}
