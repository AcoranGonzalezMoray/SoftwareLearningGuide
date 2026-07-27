namespace SoftwareLearningGuide.Core.Business.DomainEvents;

/// <summary>
/// Evento de dominio que se publica cuando se crea un nuevo Product.
/// </summary>
public sealed record ProductCreatedDomainEvent : IDomainEvent {
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public required Guid ProductId { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required string Currency { get; init; }
}
