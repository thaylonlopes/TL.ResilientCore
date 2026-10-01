using TL.ResilientCore.Application.Abstractions.Caching;

namespace TL.ResilientCore.Infrastructure.Services.Cache;

public sealed class TlCacheServiceAdapter(Caching.Helpers.Interfaces.ICacheService cacheService)
    : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return cacheService.GetAsync<T>(key);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var resolvedExpiration = expiration ?? DefaultExpiration;
        return cacheService.SetAsync(key, value, resolvedExpiration);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return cacheService.RemoveAsync(key);
    }
}
