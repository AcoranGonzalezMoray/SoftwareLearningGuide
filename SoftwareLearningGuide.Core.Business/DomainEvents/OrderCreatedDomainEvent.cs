namespace SoftwareLearningGuide.Core.Business.DomainEvents;

/// <summary>
/// Evento de dominio que se publica cuando se crea una nueva Order.
/// Los datos son inmutables (record) para garantizar integridad.
/// </summary>
public sealed record OrderCreatedDomainEvent : IDomainEvent {
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
    public required DateTime CreatedAt { get; init; }
}
