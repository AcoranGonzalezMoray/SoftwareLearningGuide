using MassTransit;
using SoftwareLearningGuide.Contracts.IntegrationEvents;

namespace SoftwareLearningGuide.Consumer.Consumers;

/// <summary>
/// Consumer de MassTransit que procesa el evento ProductCreatedIntegrationEvent
/// desde la cola de RabbitMQ.
/// 
/// Flujo completo:
/// 1. API crea Product -> Domain Event se dispara via MediatR
/// 2. NotificationHandler publica Integration Event via IPublishEndpoint
/// 3. MassTransit guarda el mensaje en la tabla OutboxMessage (misma transacción SQL)
/// 4. OutboxProcessor lee de OutboxMessage y publica a RabbitMQ
/// 5. Este Consumer recibe el mensaje de RabbitMQ y lo procesa
/// </summary>
public sealed class ProductCreatedConsumer : IConsumer<ProductCreatedEvent> {
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(ILogger<ProductCreatedConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[ProductCreatedConsumer] Product created: ProductId={ProductId}, Name={Name}, Price={Price}",
            message.ProductId,
            message.Name,
            message.Price);

        await Task.CompletedTask;
    }
}
