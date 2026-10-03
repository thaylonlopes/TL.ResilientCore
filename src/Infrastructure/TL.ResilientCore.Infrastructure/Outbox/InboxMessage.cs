namespace TL.ResilientCore.Infrastructure.Outbox;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime ReceivedOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
