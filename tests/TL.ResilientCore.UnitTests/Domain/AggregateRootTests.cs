using FluentAssertions;
using System.Linq;
using TL.ResilientCore.Domain.Primitives;
using Xunit;

namespace TL.ResilientCore.UnitTests.Domain;

public class AggregateRootTests
{
  public record DummyEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    }
    
    public class DummyAggregate : AggregateRoot
    {
        public void FazerAlgoQueGeraEvento()
        {
            RaiseDomainEvent(new DummyEvent());
        }
    }

    [Fact]
    public void RaiseDomainEvent_DeveAdicionarEvento_NaLista()
    {

        var aggregate = new DummyAggregate();

        aggregate.FazerAlgoQueGeraEvento();

        var domainEvents = aggregate.GetDomainEvents();
        domainEvents.Should().HaveCount(1);
        domainEvents.First().Should().BeOfType<DummyEvent>();
    }

    [Fact]
    public void ClearDomainEvents_DeveLimparALista_DeEventos()
    {

        var aggregate = new DummyAggregate();
        aggregate.FazerAlgoQueGeraEvento();

        aggregate.ClearDomainEvents();

        var domainEvents = aggregate.GetDomainEvents();
        domainEvents.Should().BeEmpty();
    }
}