using MassTransit;
using SoftwareLearningGuide.Contracts.IntegrationEvents;

namespace SoftwareLearningGuide.Consumer.Consumers;

/// <summary>
/// Consumer de MassTransit que procesa el evento ProductStockLowIntegrationEvent.
/// Ideal para alertas de reaprovisionamiento automatizado.
/// </summary>
public sealed class ProductStockLowConsumer : IConsumer<ProductStockLowEvent> {
    private readonly ILogger<ProductStockLowConsumer> _logger;

    public ProductStockLowConsumer(ILogger<ProductStockLowConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductStockLowEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[ProductStockLowConsumer] Low stock alert: ProductId={ProductId}, CurrentStock={CurrentStock}",
            message.ProductId,
            message.CurrentStock);

        await Task.CompletedTask;
    }
}
