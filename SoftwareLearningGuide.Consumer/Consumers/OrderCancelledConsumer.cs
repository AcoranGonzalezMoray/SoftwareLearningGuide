using MassTransit;
using SoftwareLearningGuide.Contracts.IntegrationEvents;

namespace SoftwareLearningGuide.Consumer.Consumers;

/// <summary>
/// Consumer de MassTransit que procesa el evento OrderCancelledIntegrationEvent.
/// </summary>
public sealed class OrderCancelledConsumer : IConsumer<OrderCancelledEvent> {
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(ILogger<OrderCancelledConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[OrderCancelledConsumer] Order cancelled: OrderId={OrderId}, Reason={Reason}",
            message.OrderId,
            message.Reason);

        await Task.CompletedTask;
    }
}
