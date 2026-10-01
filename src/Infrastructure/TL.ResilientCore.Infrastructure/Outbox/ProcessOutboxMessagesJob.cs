using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TL.ResilientCore.Application.Messaging;
using TL.ResilientCore.Domain.Primitives;
using TL.ResilientCore.Infrastructure.Persistence;

namespace TL.ResilientCore.Infrastructure.Outbox;

/// <summary>
/// BackgroundService encarregado de ler mensagens pendentes na Outbox e publicá-las no barramento assíncrono.
/// </summary>
public class ProcessOutboxMessagesJob : BackgroundService
{
    private const int MaxRetries = 5;
    private const int BatchSize = 20;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public ProcessOutboxMessagesJob(IServiceProvider serviceProvider, ILogger<ProcessOutboxMessagesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro catastrófico no processamento da Outbox.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        if (!messages.Any()) return;

        foreach (var message in messages)
        {
            try
            {
                message.RetryCount++;

                var domainEventType = Type.GetType(message.Type);
                
                if (domainEventType is null)
                {
                    _logger.LogWarning("Tipo do evento não encontrado: {Type} para mensagem {Id} (Tentativa {RetryCount}/{MaxRetries})", 
                        message.Type, message.Id, message.RetryCount, MaxRetries);
                    message.Error = $"Tipo do evento não encontrado: {message.Type}";
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, domainEventType) as IDomainEvent;

                if (domainEvent is null)
                {
                    _logger.LogWarning("Falha ao deserializar evento do tipo: {Type} para mensagem {Id} (Tentativa {RetryCount}/{MaxRetries})", 
                        message.Type, message.Id, message.RetryCount, MaxRetries);
                    message.Error = "Falha ao deserializar conteúdo JSON para IDomainEvent.";
                    continue;
                }

                await eventPublisher.PublishAsync(domainEvent, stoppingToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem outbox {Id} (Tentativa {RetryCount}/{MaxRetries})", 
                    message.Id, message.RetryCount, MaxRetries);
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}