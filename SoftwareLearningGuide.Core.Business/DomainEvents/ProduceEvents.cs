namespace SoftwareLearningGuide.Core.Business.DomainEvents;

/// <summary>
/// Clase base para Aggregate Roots y Entidades que soporta Domain Events.
/// Cada evento se acumula en memoria y se despacha atómicamente
/// antes de confirmar la transacción SQL (via SaveChangesInterceptor).
/// </summary>
public abstract class ProduceEvents {
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Lista de solo lectura de eventos de dominio pendientes de despacho.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Añade un evento de dominio a la cola pendiente.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent) {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Elimina un evento específico de la cola pendiente.
    /// </summary>
    public void RemoveDomainEvent(IDomainEvent domainEvent) {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Limpia todos los eventos de dominio pendientes.
    /// Se invoca después de que todos los eventos han sido despachados.
    /// </summary>
    public void ClearDomainEvents() {
        _domainEvents.Clear();
    }
}
