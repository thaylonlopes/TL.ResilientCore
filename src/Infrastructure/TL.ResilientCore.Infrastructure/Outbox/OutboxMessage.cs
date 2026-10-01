namespace TL.ResilientCore.Infrastructure.Outbox;

/// <summary>
/// Representa uma mensagem transacional pendente ou processada na tabela Outbox.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Identificador único da mensagem (compartilhado com o EventId do DomainEvent).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nome qualificado do tipo do evento para deserialização tipada.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Conteúdo do evento serializado em formato JSON.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Data e hora em UTC em que o evento de domínio ocorreu.
    /// </summary>
    public DateTime OccurredOnUtc { get; set; }

    /// <summary>
    /// Data e hora em UTC em que o evento foi processado com sucesso pelo background worker.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// Mensagem de erro da última tentativa com falha, se houver.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Quantidade de tentativas de processamento já realizadas.
    /// </summary>
    public int RetryCount { get; set; }
}