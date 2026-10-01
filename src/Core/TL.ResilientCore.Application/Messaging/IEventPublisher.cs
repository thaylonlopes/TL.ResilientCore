namespace TL.ResilientCore.Application.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(object domainEvent, CancellationToken cancellationToken = default);
}
