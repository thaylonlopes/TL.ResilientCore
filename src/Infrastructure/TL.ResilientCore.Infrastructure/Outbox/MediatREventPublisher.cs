using MediatR;
using TL.ResilientCore.Application.Messaging;
using TL.ResilientCore.Domain.Primitives;

namespace TL.ResilientCore.Infrastructure.Outbox;

public class MediatREventPublisher : IEventPublisher
{
    private readonly IPublisher _publisher;

    public MediatREventPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishAsync(object domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is IDomainEvent typedDomainEvent)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, typedDomainEvent) as INotification;
            if (notification is not null)
            {
                await _publisher.Publish(notification, cancellationToken);
            }
        }
    }
}
