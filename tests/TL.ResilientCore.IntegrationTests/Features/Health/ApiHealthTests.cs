using System.Net;
using TL.ResilientCore.IntegrationTests.Setup;
using Xunit;

namespace TL.ResilientCore.IntegrationTests.Features.Health;

public class ApiHealthTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;

    public ApiHealthTests(IntegrationTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Api_DeveResponderRequisicoesHttp()
    {
        var response = await _client.GetAsync("/");

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}