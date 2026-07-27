namespace SoftwareLearningGuide.Core.Business.Aggregates;

/// <summary>
/// Estados posibles de un Pedido en el dominio.
/// </summary>
public enum OrderStatus {
    /// <summary>Pendiente de confirmación</summary>
    Pending = 0,

    /// <summary>Confirmado por el cliente</summary>
    Confirmed = 1,

    /// <summary>En camino hacia el cliente</summary>
    Shipped = 2,

    /// <summary>Entregado al cliente</summary>
    Delivered = 3,

    /// <summary>Cancelado</summary>
    Cancelled = 4
}
