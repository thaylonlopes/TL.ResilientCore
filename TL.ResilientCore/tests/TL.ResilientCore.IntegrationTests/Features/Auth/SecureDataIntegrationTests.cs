using System.Net;
using TL.ResilientCore.IntegrationTests.Setup;
using Xunit;

namespace TL.ResilientCore.IntegrationTests.Features.Auth;

public class SecureDataIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;

    public SecureDataIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecureData_SemTokenJWT_DeveRetornar401Unauthorized()
    {
        var response = await _client.GetAsync("/secure-data");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

