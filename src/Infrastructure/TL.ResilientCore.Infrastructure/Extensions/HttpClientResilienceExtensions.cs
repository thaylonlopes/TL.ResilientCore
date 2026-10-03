using Microsoft.Extensions.DependencyInjection;
using TL.Resilience;

namespace TL.ResilientCore.Infrastructure.Extensions;

/// <summary>
/// Métodos de extensão para configuração de clientes HTTP resilientes utilizando Polly v8.
/// </summary>
public static class HttpClientResilienceExtensions
{
    /// <summary>
    /// Registra um cliente HTTP resiliente no contêiner de injeção de dependência aplicando as políticas
    /// padronizadas da biblioteca TL.Resilience (Retry exponencial com jitter decorrelacionado, Circuit Breaker e Timeout).
    /// </summary>
    /// <typeparam name="TClient">Contrato de interface do cliente HTTP.</typeparam>
    /// <typeparam name="TImplementation">Implementação concreta do cliente HTTP.</typeparam>
    /// <param name="services">Coleção de serviços de injeção de dependência.</param>
    /// <param name="configureClient">Ação opcional para configuração adicional do cliente HTTP.</param>
    /// <returns>O construtor do cliente HTTP para encadeamento fluente.</returns>
    public static IHttpClientBuilder AddResilientHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = configureClient is not null
            ? services.AddHttpClient<TClient, TImplementation>(configureClient)
            : services.AddHttpClient<TClient, TImplementation>();

        builder.AddStandardResilience();

        return builder;
    }

    /// <summary>
    /// Registra um cliente HTTP resiliente tipado no contêiner de injeção de dependência aplicando as políticas
    /// padronizadas da biblioteca TL.Resilience.
    /// </summary>
    /// <typeparam name="TClient">Classe concreta do cliente HTTP.</typeparam>
    /// <param name="services">Coleção de serviços de injeção de dependência.</param>
    /// <param name="configureClient">Ação opcional para configuração adicional do cliente HTTP.</param>
    /// <returns>O construtor do cliente HTTP para encadeamento fluente.</returns>
    public static IHttpClientBuilder AddResilientHttpClient<TClient>(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = configureClient is not null
            ? services.AddHttpClient<TClient>(configureClient)
            : services.AddHttpClient<TClient>();

        builder.AddStandardResilience();

        return builder;
    }
}
