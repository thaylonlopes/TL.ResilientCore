using FluentAssertions;
using MediatR;
using TL.ResilientCore.Application.Abstractions.Caching;
using TL.ResilientCore.Application.Behaviors;
using Xunit;

namespace TL.ResilientCore.UnitTests.Behaviors;

public sealed class CachingBehaviorTests
{
    [Fact]
    public async Task Handle_QuandoCacheHit_DeveRetornarValorDoCacheENaoChamarHandler()
    {
        var cacheService = new FakeCacheService();
        cacheService.Storage["key-existente"] = "resposta-em-cache";
        var behavior = new CachingBehavior<TestCachedQuery, string>(cacheService);
        var query = new TestCachedQuery("key-existente");
        var handlerCalled = false;

        var result = await behavior.Handle(
            query, 
            _ => { handlerCalled = true; return Task.FromResult("resposta-handler"); }, 
            CancellationToken.None);

        result.Should().Be("resposta-em-cache");
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoCacheMiss_DeveChamarHandlerEArmazenarNoCache()
    {
        var cacheService = new FakeCacheService();
        var behavior = new CachingBehavior<TestCachedQuery, string>(cacheService);
        var query = new TestCachedQuery("key-miss", TimeSpan.FromMinutes(2));
        var handlerCalled = false;

        var result = await behavior.Handle(
            query, 
            _ => { handlerCalled = true; return Task.FromResult("resposta-handler"); }, 
            CancellationToken.None);

        result.Should().Be("resposta-handler");
        handlerCalled.Should().BeTrue();
        cacheService.Storage.Should().ContainKey("key-miss");
        cacheService.Storage["key-miss"].Should().Be("resposta-handler");
        cacheService.SetCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_QuandoOcorreExcecaoNoCache_DeveExecutarGracefulFallbackEChamarHandler()
    {
        var cacheService = new FakeCacheService { ShouldThrowOnGet = true };
        var behavior = new CachingBehavior<TestCachedQuery, string>(cacheService);
        var query = new TestCachedQuery("key-com-erro");
        var handlerCalled = false;

        var result = await behavior.Handle(
            query, 
            _ => { handlerCalled = true; return Task.FromResult("resposta-com-falha-de-cache"); }, 
            CancellationToken.None);

        result.Should().Be("resposta-com-falha-de-cache");
        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_QuandoRequisicaoNaoImplementaICachedQuery_DeveIgnorarCacheEChamarHandler()
    {
        var cacheService = new FakeCacheService();
        var behavior = new CachingBehavior<NonCachedQuery, string>(cacheService);
        var query = new NonCachedQuery();
        var handlerCalled = false;

        var result = await behavior.Handle(
            query, 
            _ => { handlerCalled = true; return Task.FromResult("resposta-direta"); }, 
            CancellationToken.None);

        result.Should().Be("resposta-direta");
        handlerCalled.Should().BeTrue();
        cacheService.SetCallCount.Should().Be(0);
    }

    private sealed record TestCachedQuery(string CacheKey, TimeSpan? Expiration = null) : ICachedQuery<string>;

    private sealed record NonCachedQuery : IRequest<string>;

    private sealed class FakeCacheService : ICacheService
    {
        public Dictionary<string, object> Storage { get; } = new();
        public bool ShouldThrowOnGet { get; set; }
        public bool ShouldThrowOnSet { get; set; }
        public int SetCallCount { get; private set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (ShouldThrowOnGet)
            {
                throw new InvalidOperationException("Falha de leitura simulada no cache.");
            }

            if (Storage.TryGetValue(key, out var val) && val is T typedVal)
            {
                return Task.FromResult<T?>(typedVal);
            }

            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (ShouldThrowOnSet)
            {
                throw new InvalidOperationException("Falha de gravação simulada no cache.");
            }

            SetCallCount++;
            if (value is not null)
            {
                Storage[key] = value;
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Storage.Remove(key);
            return Task.CompletedTask;
        }
    }
}
