using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Command.CreateProduct;

/// <summary>
/// Command para crear un nuevo Product.
/// Implementa IRequest para MediatR: el controller despacha, el handler resuelve.
/// </summary>
public sealed record CreateProductCommand : IRequest<Result<Guid>> {
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public int StockQuantity { get; init; }
}
