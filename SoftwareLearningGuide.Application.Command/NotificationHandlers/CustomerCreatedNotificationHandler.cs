using MediatR;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Contracts.IntegrationEvents;
using SoftwareLearningGuide.Core.Business.DomainEvents;

namespace SoftwareLearningGuide.Application.Command.NotificationHandlers;

/// <summary>
/// Handler de MediatR que escucha el Domain Event CustomerCreatedDomainEvent
/// y registra un Integration Event en la tabla DomainOutboxMessages.
/// El OutboxProcessor se encarga de publicarlo a RabbitMQ.
/// </summary>
public sealed class CustomerCreatedNotificationHandler : INotificationHandler<CustomerCreatedDomainEvent> {
    private readonly IOutboxWriter _outboxWriter;

    public CustomerCreatedNotificationHandler(IOutboxWriter outboxWriter) {
        _outboxWriter = outboxWriter;
    }

    public async Task Handle(CustomerCreatedDomainEvent notification, CancellationToken cancellationToken) {
        var integrationEvent = new CustomerCreatedEvent {
            CustomerId = notification.CustomerId,
            Email = notification.Email,
            Name = notification.Name
        };

        await _outboxWriter.WriteAsync(integrationEvent, cancellationToken);
    }
}
