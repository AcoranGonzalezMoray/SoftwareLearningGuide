namespace SoftwareLearningGuide.Core.Business.ValueObjects;

using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;

/// <summary>
/// Value Object que representa un ID de Producto.
/// </summary>
public record ProductId {
    public Guid Value { get; }

    public ProductId(Guid value) {
        if (value == Guid.Empty)
            throw new ArgumentException(DomainErrors.IdErrors.ProductIdCannotBeEmpty());

        Value = value;
    }

    public static ProductId Create() => new(Guid.NewGuid());

    public static Result<ProductId> From(Guid value) {
        try {
            return Result<ProductId>.Success(new ProductId(value));
        }
        catch (ArgumentException ex) {
            return Result<ProductId>.Failure(ex.Message);
        }
    }

    public override string ToString() => Value.ToString();
}
