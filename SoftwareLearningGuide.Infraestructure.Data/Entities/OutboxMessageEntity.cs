namespace SoftwareLearningGuide.Infraestructure.Data.Entities;

/// <summary>
/// Representa un mensaje retenido en la tabla Outbox transaccional
/// antes de ser publicado hacia RabbitMQ por el OutboxProcessor.
/// </summary>
public sealed class OutboxMessageEntity {
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
