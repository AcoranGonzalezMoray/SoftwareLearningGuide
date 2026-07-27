namespace SoftwareLearningGuide.OutboxProcessor.Workers;

public sealed class SelectOutboxMessage {
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; }
}
