using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TL.ResilientCore.Infrastructure.Persistence;
using TL.ResilientCore.Infrastructure.Services.Inbox;
using Xunit;

namespace TL.ResilientCore.UnitTests.Inbox;

public class InboxServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task HasBeenProcessedAsync_QuandoMensagemNaoExiste_DeveRetornarFalse()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new InboxService(dbContext);

        var result = await service.HasBeenProcessedAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_DeveRegistrarMensagemComoProcessada()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new InboxService(dbContext);
        var messageId = Guid.NewGuid();

        await service.MarkAsProcessedAsync(messageId, "ClienteCriadoIntegrationEvent");

        var hasBeenProcessed = await service.HasBeenProcessedAsync(messageId);
        hasBeenProcessed.Should().BeTrue();

        var message = await dbContext.InboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        message.Should().NotBeNull();
        message!.Type.Should().Be("ClienteCriadoIntegrationEvent");
        message.ProcessedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_QuandoChamadoDuasVezes_DeveSerIdempotente()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new InboxService(dbContext);
        var messageId = Guid.NewGuid();

        await service.MarkAsProcessedAsync(messageId, "ClienteCriadoIntegrationEvent");
        await service.MarkAsProcessedAsync(messageId, "ClienteCriadoIntegrationEvent");

        var count = await dbContext.InboxMessages.CountAsync(m => m.Id == messageId);
        count.Should().Be(1);
    }
}
