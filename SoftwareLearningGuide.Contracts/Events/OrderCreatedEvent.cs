namespace SoftwareLearningGuide.Contracts.IntegrationEvents;

public sealed record OrderCreatedEvent {
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
