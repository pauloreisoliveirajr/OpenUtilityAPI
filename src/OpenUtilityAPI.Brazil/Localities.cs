using System.Collections.Frozen;

namespace OpenUtilityAPI.Brazil;

public static class StateCodes
{
    public static IReadOnlyDictionary<string, int> All { get; } = new Dictionary<string, int>
    {
        ["AC"] = 12, ["AL"] = 27, ["AP"] = 16, ["AM"] = 13, ["BA"] = 29, ["CE"] = 23,
        ["DF"] = 53, ["ES"] = 32, ["GO"] = 52, ["MA"] = 21, ["MT"] = 51, ["MS"] = 50,
        ["MG"] = 31, ["PA"] = 15, ["PB"] = 25, ["PR"] = 41, ["PE"] = 26, ["PI"] = 22,
        ["RJ"] = 33, ["RN"] = 24, ["RS"] = 43, ["RO"] = 11, ["RR"] = 14, ["SC"] = 42,
        ["SP"] = 35, ["SE"] = 28, ["TO"] = 17
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static string Normalize(string state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var normalized = state.ToUpperInvariant();
        if (!All.ContainsKey(normalized))
            throw new ArgumentException("Use a Brazilian state abbreviation, for example SP or DF.", nameof(state));
        return normalized;
    }
}

public sealed record BrazilianState(int Id, string Abbreviation, string Name, int RegionId, string RegionName);

public sealed record Municipality(int Id, string Name, string State);

public interface ILocalityLookup
{
    Task<IReadOnlyList<BrazilianState>> GetStatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(string state, CancellationToken cancellationToken = default);
}

public sealed class LocalityLookupException(string message, bool isTimeout = false, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsTimeout { get; } = isTimeout;
}
