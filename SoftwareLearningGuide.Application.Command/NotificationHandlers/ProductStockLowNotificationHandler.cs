using MediatR;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Contracts.IntegrationEvents;
using SoftwareLearningGuide.Core.Business.DomainEvents;

namespace SoftwareLearningGuide.Application.Command.NotificationHandlers;

/// <summary>
/// Handler de MediatR que escucha el Domain Event ProductStockLowDomainEvent
/// y registra un Integration Event en la tabla DomainOutboxMessages.
/// Ideal para alertas de reaprovisionamiento automatizado.
/// </summary>
public sealed class ProductStockLowNotificationHandler : INotificationHandler<ProductStockLowDomainEvent> {
    private readonly IOutboxWriter _outboxWriter;

    public ProductStockLowNotificationHandler(IOutboxWriter outboxWriter) {
        _outboxWriter = outboxWriter;
    }

    public async Task Handle(ProductStockLowDomainEvent notification, CancellationToken cancellationToken) {
        var integrationEvent = new ProductStockLowEvent {
            ProductId = notification.ProductId,
            CurrentStock = notification.CurrentStock
        };

        await _outboxWriter.WriteAsync(integrationEvent, cancellationToken);
    }
}
