using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.ResilientCore.Infrastructure.Persistence;
using TL.ResilientCore.IntegrationTests.Setup;
using Xunit;

namespace TL.ResilientCore.IntegrationTests.Features.Health;

public class HealthCheckIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public HealthCheckIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostgresContainer_DeveEstarAcessivel_EPermitirConexaoComBanco()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var canConnect = await dbContext.Database.CanConnectAsync();

        Assert.True(canConnect, "A instância efêmera do PostgreSQL gerenciada pelo Testcontainers não pôde ser acessada.");
    }
}