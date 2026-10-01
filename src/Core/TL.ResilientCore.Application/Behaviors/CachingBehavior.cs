using MediatR;
using TL.ResilientCore.Application.Abstractions.Caching;

namespace TL.ResilientCore.Application.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse>(ICacheService cacheService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICachedQuery<TResponse> cachedQuery)
        {
            return await next();
        }

        var cachedResponse = await TryGetFromCacheAsync(cachedQuery, cancellationToken);
        if (cachedResponse is not null)
        {
            return cachedResponse;
        }

        var response = await next();

        await TrySetInCacheAsync(cachedQuery, response, cancellationToken);

        return response;
    }

    private async Task<TResponse?> TryGetFromCacheAsync(
        ICachedQuery<TResponse> cachedQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            return await cacheService.GetAsync<TResponse>(cachedQuery.CacheKey, cancellationToken);
        }
        catch
        {
            return default;
        }
    }

    private async Task TrySetInCacheAsync(
        ICachedQuery<TResponse> cachedQuery,
        TResponse? response,
        CancellationToken cancellationToken)
    {
        if (response is null)
        {
            return;
        }

        try
        {
            await cacheService.SetAsync(
                cachedQuery.CacheKey,
                response,
                cachedQuery.Expiration,
                cancellationToken);
        }
        catch
        {
            return;
        }
    }
}
