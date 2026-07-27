namespace SoftwareLearningGuide.Contracts.IntegrationEvents;

public sealed record CustomerCreatedEvent {
    public Guid CustomerId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
