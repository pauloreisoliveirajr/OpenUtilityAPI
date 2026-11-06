using Microsoft.Extensions.Caching.Memory;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>A bounded, process-local cache for normalized postal address lookups.</summary>
public sealed class PostalAddressCache : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 1024 });
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Read a cached address or call the provider once for concurrent requests.</summary>
    public async Task<PostalAddress?> FindAsync(string postalCode, IPostalAddressLookup provider,
        CancellationToken cancellationToken)
    {
        var normalized = PostalCodes.Normalize(postalCode);
        if (cache.TryGetValue(normalized, out CacheEntry? entry))
            return entry!.Address;

        // Serialize cache misses to bound outbound concurrency and avoid duplicate provider calls.
        if (!await gate.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            throw new PostalLookupException("The postal address provider is busy.", isTimeout: true);
        try
        {
            if (cache.TryGetValue(normalized, out entry))
                return entry!.Address;

            var address = await provider.FindAsync(normalized, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            cache.Set(normalized, new CacheEntry(address), new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = address is null ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(1)
            });
            return address;
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

    private sealed record CacheEntry(PostalAddress? Address);
}
