namespace SoftwareLearningGuide.Contracts.IntegrationEvents;

public sealed record ProductCreatedEvent {
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
}
