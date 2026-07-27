using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Command.CreateOrder;

/// <summary>
/// Command para crear una nueva Order.
/// Implementa IRequest para MediatR: el controller despacha, el handler resuelve.
/// </summary>
public sealed record CreateOrderCommand : IRequest<Result<Guid>> {
    public Guid CustomerId { get; init; }
    public string ShippingStreet { get; init; } = string.Empty;
    public string ShippingCity { get; init; } = string.Empty;
    public string ShippingState { get; init; } = string.Empty;
    public string ShippingPostalCode { get; init; } = string.Empty;
    public string ShippingCountry { get; init; } = string.Empty;
    public List<CreateOrderLineCommand> Lines { get; init; } = new();
}

/// <summary>
/// Command para agregar una línea a una Order.
/// </summary>
public sealed record CreateOrderLineCommand {
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
