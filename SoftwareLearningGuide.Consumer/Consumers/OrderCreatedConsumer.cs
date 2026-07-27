using MassTransit;
using SoftwareLearningGuide.Contracts.IntegrationEvents;

namespace SoftwareLearningGuide.Consumer.Consumers;

/// <summary>
/// Consumer de MassTransit que procesa el evento OrderCreatedIntegrationEvent
/// desde la cola de RabbitMQ.
/// 
/// Flujo completo:
/// 1. API crea Order -> Domain Event se dispara via MediatR
/// 2. NotificationHandler publica Integration Event via IPublishEndpoint
/// 3. MassTransit guarda el mensaje en la tabla OutboxMessage (misma transacción SQL)
/// 4. OutboxProcessor lee de OutboxMessage y publica a RabbitMQ
/// 5. Este Consumer recibe el mensaje de RabbitMQ y lo procesa
/// </summary>
public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent> {
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[OrderCreatedConsumer] Order created: OrderId={OrderId}, CustomerId={CustomerId}, Total={TotalAmount} {Currency}",
            message.OrderId,
            message.CustomerId,
            message.TotalAmount,
            message.Currency);

        await Task.CompletedTask;
    }
}
