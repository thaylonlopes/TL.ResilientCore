using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.ResilientCore.Infrastructure.Persistence;
using TL.ResilientCore.IntegrationTests.Setup;
using Xunit;

namespace TL.ResilientCore.IntegrationTests.Features.Database;

public class DatabaseMigrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public DatabaseMigrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_DeveAplicarMigrations_SemErros()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendingMigrations);
    }
}