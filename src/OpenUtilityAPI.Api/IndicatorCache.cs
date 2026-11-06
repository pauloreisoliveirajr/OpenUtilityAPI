using Microsoft.Extensions.Caching.Memory;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>A bounded process-local cache for the supported indicator observations.</summary>
public sealed class IndicatorCache : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 3 });
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Get an observation cached for 15 minutes or a missing result for five minutes.</summary>
    public async Task<IndicatorObservation?> GetLatestAsync(string code, IIndicatorLookup provider,
        CancellationToken cancellationToken)
    {
        var indicator = Indicators.Find(code)
            ?? throw new ArgumentException("Select an indicator from the supported catalog.", nameof(code));
        cancellationToken.ThrowIfCancellationRequested();
        if (cache.TryGetValue(indicator.Code, out CacheEntry? entry))
            return entry!.Observation;

        if (!await gate.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            throw new IndicatorLookupException("The indicator provider is busy.", isTimeout: true);
        try
        {
            if (cache.TryGetValue(indicator.Code, out entry))
                return entry!.Observation;

            var observation = await provider.GetLatestAsync(indicator.Code, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            cache.Set(indicator.Code, new CacheEntry(observation), new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = observation is null ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(15)
            });
            return observation;
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

    private sealed record CacheEntry(IndicatorObservation? Observation);
}
