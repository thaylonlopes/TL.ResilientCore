using System.Reflection;
using Microsoft.Extensions.Logging;
using TL.BaseContracts.Messaging;
using TL.ResilientCore.Application.Messaging;

namespace TL.ResilientCore.Infrastructure.Outbox;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IEventProducer? _eventProducer;
    private readonly MediatREventPublisher _mediatREventPublisher;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    private static readonly MethodInfo PublishMethod = typeof(IEventProducer)
        .GetMethods()
        .First(m => m.Name == nameof(IEventProducer.PublishAsync) && m.GetParameters().Length == 2 && m.GetGenericArguments().Length == 1);

    public RabbitMqEventPublisher(
        MediatREventPublisher mediatREventPublisher,
        ILogger<RabbitMqEventPublisher> logger,
        IEventProducer? eventProducer = null)
    {
        _mediatREventPublisher = mediatREventPublisher ?? throw new ArgumentNullException(nameof(mediatREventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventProducer = eventProducer;
    }

    public async Task PublishAsync(object domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        using var activity = Diagnostics.Telemetry.ActivitySource.StartActivity(
            $"Publish {domainEvent.GetType().Name}",
            System.Diagnostics.ActivityKind.Producer);

        if (activity is not null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.destination_kind", "exchange");
            activity.SetTag("messaging.event_type", domainEvent.GetType().Name);
        }

        if (_eventProducer is not null)
        {
            try
            {
                await PublishToBrokerAsync(domainEvent, cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha na publicação de evento via RabbitMQ. Executando fallback para MediatR.");
            }
        }

        await _mediatREventPublisher.PublishAsync(domainEvent, cancellationToken);
    }

    private async Task PublishToBrokerAsync(object domainEvent, CancellationToken cancellationToken)
    {
        var genericPublishMethod = PublishMethod.MakeGenericMethod(domainEvent.GetType());
        var publishTask = (Task)genericPublishMethod.Invoke(_eventProducer, [domainEvent, cancellationToken])!;
        await publishTask.ConfigureAwait(false);
    }
}
