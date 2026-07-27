namespace SoftwareLearningGuide.Core.Business.DomainEvents;

/// <summary>
/// Evento de dominio que se publica cuando se cancela una Order.
/// </summary>
public sealed record OrderCancelledDomainEvent : IDomainEvent {
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public required Guid OrderId { get; init; }
    public required string Reason { get; init; }
    public required DateTime CancelledAt { get; init; }
}
