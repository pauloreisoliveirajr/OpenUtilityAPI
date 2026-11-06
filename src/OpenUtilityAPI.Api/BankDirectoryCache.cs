using Microsoft.Extensions.Caching.Memory;
using OpenUtilityAPI.Brazil;

namespace OpenUtilityAPI.Api;

/// <summary>A bounded process-local cache shared by bank listing and code lookups.</summary>
public sealed class BankDirectoryCache : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 1 });
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Get a bank directory snapshot cached for 24 hours.</summary>
    public async Task<IReadOnlyList<Bank>> GetBanksAsync(IBankLookup provider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (cache.TryGetValue("banks", out IReadOnlyList<Bank>? result))
            return result!;

        if (!await gate.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            throw new BankLookupException("The bank directory provider is busy.", isTimeout: true);
        try
        {
            if (cache.TryGetValue("banks", out result))
                return result!;

            var banks = await provider.GetBanksAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            result = Array.AsReadOnly(banks.ToArray());
            cache.Set("banks", result, new MemoryCacheEntryOptions
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
