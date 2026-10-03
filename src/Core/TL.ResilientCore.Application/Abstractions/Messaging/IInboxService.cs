namespace TL.ResilientCore.Application.Abstractions.Messaging;

public interface IInboxService
{
    Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, string eventType, CancellationToken cancellationToken = default);
}
