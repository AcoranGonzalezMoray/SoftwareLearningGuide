namespace SoftwareLearningGuide.Core.Business.DomainEvents;

/// <summary>
/// Evento de dominio que se publica cuando se crea un nuevo Customer.
/// </summary>
public sealed record CustomerCreatedDomainEvent : IDomainEvent {
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public required Guid CustomerId { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
}
