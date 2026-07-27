namespace SoftwareLearningGuide.Core.Business.ValueObjects;

using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;

/// <summary>
/// Value Object que representa un ID de Línea de Pedido.
/// </summary>
public record OrderLineId {
    public Guid Value { get; }

    public OrderLineId(Guid value) {
        if (value == Guid.Empty)
            throw new ArgumentException(DomainErrors.IdErrors.OrderLineIdCannotBeEmpty());

        Value = value;
    }

    public static OrderLineId Create() => new(Guid.NewGuid());

    public static Result<OrderLineId> From(Guid value) {
        try {
            return Result<OrderLineId>.Success(new OrderLineId(value));
        }
        catch (ArgumentException ex) {
            return Result<OrderLineId>.Failure(ex.Message);
        }
    }

    public override string ToString() => Value.ToString();
}
