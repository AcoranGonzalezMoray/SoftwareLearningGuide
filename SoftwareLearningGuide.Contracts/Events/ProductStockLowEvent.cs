namespace SoftwareLearningGuide.Contracts.IntegrationEvents;

public sealed record ProductStockLowEvent {
    public Guid ProductId { get; init; }
    public int CurrentStock { get; init; }
}
