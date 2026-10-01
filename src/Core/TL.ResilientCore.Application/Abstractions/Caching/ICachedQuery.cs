using MediatR;

namespace TL.ResilientCore.Application.Abstractions.Caching;

public interface ICachedQuery<out TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}
