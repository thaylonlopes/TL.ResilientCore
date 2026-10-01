using MediatR;
using TL.ResilientCore.Domain.Primitives;

namespace TL.ResilientCore.Application.Messaging;

public interface IDomainEventHandler<TEvent> : INotificationHandler<DomainEventNotification<TEvent>>
    where TEvent : IDomainEvent
{
}