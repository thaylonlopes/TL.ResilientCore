using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TL.Resilience;
using TL.ResilientCore.Infrastructure.Extensions;
using Xunit;

namespace TL.ResilientCore.UnitTests.Resilience;

public sealed class HttpClientResilienceTests
{
    public interface ITestExternalService
    {
        HttpClient Client { get; }
        Task<HttpResponseMessage> GetDataAsync(CancellationToken cancellationToken = default);
    }

    public sealed class TestExternalService : ITestExternalService
    {
        public HttpClient Client { get; }

        public TestExternalService(HttpClient client)
        {
            Client = client;
        }

        public async Task<HttpResponseMessage> GetDataAsync(CancellationToken cancellationToken = default)
        {
            return await Client.GetAsync("/api/data", cancellationToken);
        }
    }

    [Fact]
    public void AddResilientHttpClient_DeveRegistrarClientEHandlerNoServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddResilientHttpClient<ITestExternalService, TestExternalService>();

        services.Should().Contain(sd => sd.ServiceType == typeof(ITestExternalService));
        services.Should().Contain(sd => sd.ServiceType == typeof(StandardResilienceHandler));
    }

    [Fact]
    public void AddResilientHttpClient_ComOverloadSimples_DeveRegistrarClientEHandler()
    {
        var services = new ServiceCollection();

        services.AddResilientHttpClient<TestExternalService>();

        services.Should().Contain(sd => sd.ServiceType == typeof(TestExternalService));
        services.Should().Contain(sd => sd.ServiceType == typeof(StandardResilienceHandler));
    }

    [Fact]
    public void AddResilientHttpClient_QuandoConfiguracaoCustomizadaInformada_DeveAplicarAoHttpClient()
    {
        var services = new ServiceCollection();
        var baseUri = new Uri("https://api.empresa.local");

        services.AddResilientHttpClient<ITestExternalService, TestExternalService>(client =>
        {
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Add("X-Custom-Header", "ResilienceValue");
        });

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestExternalService>();

        service.Client.BaseAddress.Should().Be(baseUri);
        service.Client.DefaultRequestHeaders.GetValues("X-Custom-Header").Should().Contain("ResilienceValue");
    }

    [Fact]
    public async Task AddResilientHttpClient_QuandoExecucaoNormal_DeveRetornarSucesso()
    {
        var services = new ServiceCollection();
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":\"ok\"}")
        });

        services.AddResilientHttpClient<ITestExternalService, TestExternalService>(client =>
        {
            client.BaseAddress = new Uri("https://api.empresa.local");
        })
        .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestExternalService>();

        var response = await service.GetDataAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task AddResilientHttpClient_QuandoFalhaTransitoriaOcorre_DeveAplicarRetryERecuperar()
    {
        var services = new ServiceCollection();
        var handler = new FlakyHttpMessageHandler(failuresBeforeSuccess: 2);

        services.AddResilientHttpClient<ITestExternalService, TestExternalService>(client =>
        {
            client.BaseAddress = new Uri("https://api.empresa.local");
        })
        .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestExternalService>();

        var response = await service.GetDataAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.CallCount.Should().Be(3);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int CallCount { get; private set; }

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }

    private sealed class FlakyHttpMessageHandler : HttpMessageHandler
    {
        private readonly int _failuresBeforeSuccess;
        public int CallCount { get; private set; }

        public FlakyHttpMessageHandler(int failuresBeforeSuccess)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (CallCount <= _failuresBeforeSuccess)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
