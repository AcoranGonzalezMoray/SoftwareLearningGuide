namespace SoftwareLearningGuide.Contracts.IntegrationEvents;

public sealed record OrderCancelledEvent {
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
}
