using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using TL.BaseContracts.Messaging;
using TL.ResilientCore.Application.Messaging;
using TL.ResilientCore.Domain.Primitives;
using TL.ResilientCore.Infrastructure.Outbox;
using Xunit;

namespace TL.ResilientCore.UnitTests.Outbox;

public sealed class EventPublisherTests
{
    [Fact]
    public async Task PublishAsync_ComProducerDisponivel_DevePublicarNoBroker()
    {
        var fakeProducer = new FakeEventProducer();
        var fakePublisher = new FakePublisher();
        var mediatRPublisher = new MediatREventPublisher(fakePublisher);
        var rabbitMqPublisher = new RabbitMqEventPublisher(
            mediatRPublisher, 
            NullLogger<RabbitMqEventPublisher>.Instance, 
            fakeProducer);
        var domainEvent = new ClienteTestDomainEvent();

        await rabbitMqPublisher.PublishAsync(domainEvent, CancellationToken.None);

        fakeProducer.PublishedMessages.Should().ContainSingle();
        fakeProducer.PublishedMessages.First().Should().Be(domainEvent);
        fakePublisher.PublishedNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAsync_QuandoBrokerFalhar_DeveExecutarFallbackParaMediatR()
    {
        var fakeProducer = new FakeEventProducer { ShouldThrow = true };
        var fakePublisher = new FakePublisher();
        var mediatRPublisher = new MediatREventPublisher(fakePublisher);
        var rabbitMqPublisher = new RabbitMqEventPublisher(
            mediatRPublisher, 
            NullLogger<RabbitMqEventPublisher>.Instance, 
            fakeProducer);
        var domainEvent = new ClienteTestDomainEvent();

        await rabbitMqPublisher.PublishAsync(domainEvent, CancellationToken.None);

        fakeProducer.PublishedMessages.Should().BeEmpty();
        fakePublisher.PublishedNotifications.Should().ContainSingle();
        fakePublisher.PublishedNotifications.First().Should().BeOfType<DomainEventNotification<ClienteTestDomainEvent>>();
    }

    [Fact]
    public async Task PublishAsync_QuandoProducerForNulo_DevePublicarDiretamenteNoMediatR()
    {
        var fakePublisher = new FakePublisher();
        var mediatRPublisher = new MediatREventPublisher(fakePublisher);
        var rabbitMqPublisher = new RabbitMqEventPublisher(
            mediatRPublisher, 
            NullLogger<RabbitMqEventPublisher>.Instance, 
            eventProducer: null);
        var domainEvent = new ClienteTestDomainEvent();

        await rabbitMqPublisher.PublishAsync(domainEvent, CancellationToken.None);

        fakePublisher.PublishedNotifications.Should().ContainSingle();
        fakePublisher.PublishedNotifications.First().Should().BeOfType<DomainEventNotification<ClienteTestDomainEvent>>();
    }

    [Fact]
    public async Task MediatREventPublisher_DeveEncapsularEmDomainEventNotificationEPublicar()
    {
        var fakePublisher = new FakePublisher();
        var mediatRPublisher = new MediatREventPublisher(fakePublisher);
        var domainEvent = new ClienteTestDomainEvent();

        await mediatRPublisher.PublishAsync(domainEvent, CancellationToken.None);

        fakePublisher.PublishedNotifications.Should().ContainSingle();
        var notification = fakePublisher.PublishedNotifications.First().Should().BeOfType<DomainEventNotification<ClienteTestDomainEvent>>().Subject;
        notification.DomainEvent.Should().Be(domainEvent);
    }

    private sealed record ClienteTestDomainEvent(
        Guid EventId,
        DateTime OccurredOnUtc) : IDomainEvent
    {
        public ClienteTestDomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
    }

    private sealed class FakeEventProducer : IEventProducer
    {
        public List<object> PublishedMessages { get; } = new();
        public bool ShouldThrow { get; set; }

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            if (ShouldThrow)
            {
                throw new InvalidOperationException("Falha de conexão com o broker RabbitMQ.");
            }

            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(string topicOrExchange, T message, CancellationToken cancellationToken = default) where T : class
        {
            return PublishAsync(message, cancellationToken);
        }

        public Task<TL.BaseContracts.Result> PublishAsync<T>(T message, EventMetadata? metadata, CancellationToken cancellationToken = default) where T : class
        {
            PublishAsync(message, cancellationToken);
            return Task.FromResult(TL.BaseContracts.Result.Success());
        }

        public Task<TL.BaseContracts.Result> PublishBatchAsync<T>(IEnumerable<T> messages, EventMetadata? metadata = null, CancellationToken cancellationToken = default) where T : class
        {
            foreach (var message in messages)
            {
                PublishAsync(message, cancellationToken);
            }
            return Task.FromResult(TL.BaseContracts.Result.Success());
        }
    }

    private sealed class FakePublisher : IPublisher
    {
        public List<object> PublishedNotifications { get; } = new();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            PublishedNotifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            PublishedNotifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}
