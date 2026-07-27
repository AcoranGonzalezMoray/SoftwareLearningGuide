using MediatR;

namespace SoftwareLearningGuide.Core.Business.DomainEvents;

/// <summary>
/// Interfaz base para todos los eventos de dominio.
/// Implementa INotification de MediatR para permitir el despacho
/// a través de IMediator.Publish() dentro de la misma transacción.
/// </summary>
public interface IDomainEvent : INotification {
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
