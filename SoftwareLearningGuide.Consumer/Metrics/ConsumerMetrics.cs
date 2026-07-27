using System.Diagnostics.Metrics;

namespace SoftwareLearningGuide.Consumer.Metrics;

public sealed class ConsumerMetrics {
    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _messagesFailed;
    private readonly Histogram<double> _processingDuration;

    public ConsumerMetrics(IMeterFactory meterFactory) {
        var meter = meterFactory.Create("SoftwareLearningGuide.Consumer");
        _messagesProcessed = meter.CreateCounter<long>("consumer.messages.processed", "messages", "Total messages processed by consumers");
        _messagesFailed = meter.CreateCounter<long>("consumer.messages.failed", "messages", "Total messages that failed processing");
        _processingDuration = meter.CreateHistogram<double>("consumer.messages.duration", "ms", "Processing duration per message");
    }

    public void MessageProcessed(string messageType) {
        _messagesProcessed.Add(1,
            new KeyValuePair<string, object?>("message.type", messageType));
    }

    public void MessageFailed(string messageType, string error) {
        _messagesFailed.Add(1,
            new KeyValuePair<string, object?>("message.type", messageType),
            new KeyValuePair<string, object?>("error", error));
    }

    public void ProcessingDuration(string messageType, double durationMs) {
        _processingDuration.Record(durationMs,
            new KeyValuePair<string, object?>("message.type", messageType));
    }
}
