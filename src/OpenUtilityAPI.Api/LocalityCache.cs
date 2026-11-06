using Microsoft.Extensions.Caching.Memory;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>A bounded process-local cache for IBGE locality lists.</summary>
public sealed class LocalityCache : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 28 });
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Get the complete state list with a 24-hour cache lifetime.</summary>
    public Task<IReadOnlyList<BrazilianState>> GetStatesAsync(ILocalityLookup provider, CancellationToken cancellationToken) =>
        GetAsync("states", provider.GetStatesAsync, cancellationToken);

    /// <summary>Get municipalities for one normalized state abbreviation.</summary>
    public Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(string state, ILocalityLookup provider,
        CancellationToken cancellationToken)
    {
        var normalized = StateCodes.Normalize(state);
        return GetAsync(normalized, token => provider.GetMunicipalitiesAsync(normalized, token), cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetAsync<T>(string key,
        Func<CancellationToken, Task<IReadOnlyList<T>>> lookup, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (cache.TryGetValue(key, out IReadOnlyList<T>? result))
            return result!;

        if (!await gate.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            throw new LocalityLookupException("The locality provider is busy.", isTimeout: true);
        try
        {
            if (cache.TryGetValue(key, out result))
                return result!;
            var items = await lookup(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            // Copy provider lists so mutations cannot change a cached result.
            result = Array.AsReadOnly(items.ToArray());
            cache.Set(key, result, new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        cache.Dispose();
        gate.Dispose();
    }
}
