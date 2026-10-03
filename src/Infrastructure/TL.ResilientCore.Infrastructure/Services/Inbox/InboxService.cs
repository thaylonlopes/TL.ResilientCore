using Microsoft.EntityFrameworkCore;
using TL.ResilientCore.Application.Abstractions.Messaging;
using TL.ResilientCore.Infrastructure.Outbox;
using TL.ResilientCore.Infrastructure.Persistence;

namespace TL.ResilientCore.Infrastructure.Services.Inbox;

public class InboxService : IInboxService
{
    private readonly ApplicationDbContext _dbContext;

    public InboxService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InboxMessages
            .AsNoTracking()
            .AnyAsync(m => m.Id == messageId && m.ProcessedOnUtc != null, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, string eventType, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.InboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (existing is null)
        {
            _dbContext.InboxMessages.Add(new InboxMessage
            {
                Id = messageId,
                Type = eventType,
                ReceivedOnUtc = DateTime.UtcNow,
                ProcessedOnUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.ProcessedOnUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
