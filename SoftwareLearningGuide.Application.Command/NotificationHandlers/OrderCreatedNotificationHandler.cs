using MediatR;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Contracts.IntegrationEvents;
using SoftwareLearningGuide.Core.Business.DomainEvents;

namespace SoftwareLearningGuide.Application.Command.NotificationHandlers;

/// <summary>
/// Handler de MediatR que escucha el Domain Event OrderCreatedDomainEvent
/// y registra un Integration Event en la tabla DomainOutboxMessages.
/// El OutboxProcessor se encarga de publicarlo a RabbitMQ.
/// </summary>
public sealed class OrderCreatedNotificationHandler : INotificationHandler<OrderCreatedDomainEvent> {
    private readonly IOutboxWriter _outboxWriter;

    public OrderCreatedNotificationHandler(IOutboxWriter outboxWriter) {
        _outboxWriter = outboxWriter;
    }

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken) {
        var integrationEvent = new OrderCreatedEvent {
            OrderId = notification.OrderId,
            CustomerId = notification.CustomerId,
            TotalAmount = notification.TotalAmount,
            Currency = notification.Currency,
            CreatedAt = notification.CreatedAt
        };

        await _outboxWriter.WriteAsync(integrationEvent, cancellationToken);
    }
}
