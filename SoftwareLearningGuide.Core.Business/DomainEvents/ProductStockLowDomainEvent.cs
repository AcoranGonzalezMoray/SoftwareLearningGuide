namespace SoftwareLearningGuide.Core.Business.DomainEvents;

/// <summary>
/// Evento de dominio que se publica cuando el stock de un Product baja del umbral.
/// Ideal para alertas y reaprovisionamiento automatizado.
/// </summary>
public sealed record ProductStockLowDomainEvent : IDomainEvent {
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public required Guid ProductId { get; init; }
    public required int CurrentStock { get; init; }
}
