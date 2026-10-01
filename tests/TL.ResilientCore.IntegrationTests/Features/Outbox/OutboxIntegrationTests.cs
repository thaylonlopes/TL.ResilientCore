using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.ResilientCore.Infrastructure.Persistence;
using TL.ResilientCore.IntegrationTests.Setup;
using Xunit;

namespace TL.ResilientCore.IntegrationTests.Features.Outbox;

public class OutboxIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public OutboxIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OutboxMessages_DeveEstarPresenteNoModel_E_ConterPropriedadesDeResiliencia()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var outboxType = dbContext.Model.FindEntityType("TL.ResilientCore.Infrastructure.Outbox.OutboxMessage");
        
        Assert.NotNull(outboxType);
        
        var retryCountProperty = outboxType.FindProperty("RetryCount");
        Assert.NotNull(retryCountProperty);

        var errorProperty = outboxType.FindProperty("Error");
        Assert.NotNull(errorProperty);

        var count = await dbContext.OutboxMessages.CountAsync();
        Assert.True(count >= 0);
    }
}