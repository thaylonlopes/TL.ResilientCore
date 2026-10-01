namespace TL.ResilientCore.Domain.Primitives;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}