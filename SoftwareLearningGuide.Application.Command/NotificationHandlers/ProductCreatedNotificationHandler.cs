using MediatR;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Contracts.IntegrationEvents;
using SoftwareLearningGuide.Core.Business.DomainEvents;

namespace SoftwareLearningGuide.Application.Command.NotificationHandlers;

/// <summary>
/// Handler de MediatR que escucha el Domain Event ProductCreatedDomainEvent
/// y registra un Integration Event en la tabla Outbox.
/// </summary>
public sealed class ProductCreatedNotificationHandler : INotificationHandler<ProductCreatedDomainEvent> {
    private readonly IOutboxWriter _outboxWriter;

    public ProductCreatedNotificationHandler(IOutboxWriter outboxWriter) {
        _outboxWriter = outboxWriter;
    }

    public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken) {
        var integrationEvent = new ProductCreatedEvent {
            ProductId = notification.ProductId,
            Name = notification.Name,
            Price = notification.Price,
            Currency = notification.Currency
        };

        await _outboxWriter.WriteAsync(integrationEvent, cancellationToken);
    }
}
