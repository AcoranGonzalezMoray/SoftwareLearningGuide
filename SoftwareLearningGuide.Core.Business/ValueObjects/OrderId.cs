namespace SoftwareLearningGuide.Core.Business.ValueObjects;

using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;

/// <summary>
/// Value Object que representa un ID de Pedido.
/// Proporciona type-safety evitando que se mezclen IDs de diferentes dominios.
/// </summary>
public record OrderId {
    public Guid Value { get; }

    public OrderId(Guid value) {
        if (value == Guid.Empty)
            throw new ArgumentException(DomainErrors.IdErrors.OrderIdCannotBeEmpty());

        Value = value;
    }

    public static OrderId Create() => new(Guid.NewGuid());

    public static Result<OrderId> From(Guid value) {
        try {
            return Result<OrderId>.Success(new OrderId(value));
        }
        catch (ArgumentException ex) {
            return Result<OrderId>.Failure(ex.Message);
        }
    }

    public override string ToString() => Value.ToString();
}
