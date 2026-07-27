using MediatR;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Contracts.IntegrationEvents;
using SoftwareLearningGuide.Core.Business.DomainEvents;

namespace SoftwareLearningGuide.Application.Command.NotificationHandlers;

/// <summary>
/// Handler de MediatR que escucha el Domain Event OrderCancelledDomainEvent
/// y registra un Integration Event en la tabla DomainOutboxMessages.
/// El OutboxProcessor se encarga de publicarlo a RabbitMQ.
/// </summary>
public sealed class OrderCancelledNotificationHandler : INotificationHandler<OrderCancelledDomainEvent> {
    private readonly IOutboxWriter _outboxWriter;

    public OrderCancelledNotificationHandler(IOutboxWriter outboxWriter) {
        _outboxWriter = outboxWriter;
    }

    public async Task Handle(OrderCancelledDomainEvent notification, CancellationToken cancellationToken) {
        var integrationEvent = new OrderCancelledEvent {
            OrderId = notification.OrderId,
            Reason = notification.Reason,
            CancelledAt = notification.CancelledAt
        };

        await _outboxWriter.WriteAsync(integrationEvent, cancellationToken);
    }
}
