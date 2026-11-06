namespace OpenUtilityAPI.Brazil;

public static class Indicators
{
    public static IReadOnlyList<IndicatorDefinition> All { get; } = Array.AsReadOnly<IndicatorDefinition>(
    [
        new("selic-target", "Selic target", 432, "% per year", "daily"),
        new("cdi", "CDI", 12, "% per day", "daily"),
        new("ipca", "IPCA", 433, "monthly % change", "monthly")
    ]);

    public static IndicatorDefinition? Find(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return All.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record IndicatorDefinition(string Code, string Name, int SeriesId, string Unit, string ObservationFrequency)
{
    public string Source => "BCB SGS";
    public string SourceUrl => $"https://api.bcb.gov.br/dados/serie/bcdata.sgs.{SeriesId}/dados/ultimos/1?formato=json";
}

public sealed record IndicatorObservation(IndicatorDefinition Indicator, decimal Value, DateOnly ReferenceDate,
    DateTimeOffset RetrievedAtUtc);

public interface IIndicatorLookup
{
    Task<IndicatorObservation?> GetLatestAsync(string code, CancellationToken cancellationToken = default);
}

public sealed class IndicatorLookupException(string message, bool isTimeout = false, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsTimeout { get; } = isTimeout;
}
