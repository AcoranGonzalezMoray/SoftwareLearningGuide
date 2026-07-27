namespace SoftwareLearningGuide.Core.Business.Entities;

using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

/// <summary>
/// Entidad que representa una línea dentro de un Pedido (Order).
/// Forma parte del agregado Order y no debe ser persistida independientemente.
/// </summary>
public class OrderLine {
    public OrderLineId Id { get; }
    public ProductId ProductId { get; }
    public string ProductName { get; }
    public Money UnitPrice { get; }
    public int Quantity { get; private set; }
    public DateTime CreatedAt { get; }

    private OrderLine() { }

    public OrderLine(OrderLineId id, ProductId productId, string productName, Money unitPrice, int quantity) {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        if (productId == null)
            throw new ArgumentNullException(nameof(productId));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(DomainErrors.OrderLine.ProductNameCannotBeEmpty(productId.Value));

        if (unitPrice == null)
            throw new ArgumentNullException(nameof(unitPrice));

        if (unitPrice.Amount <= 0)
            throw new ArgumentException(DomainErrors.OrderLine.UnitPriceMustBeGreaterThanZero(productId.Value));

        if (quantity <= 0)
            throw new ArgumentException(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(productId.Value));

        Id = id;
        ProductId = productId;
        ProductName = productName.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Crea una instancia de OrderLine con validación.
    /// </summary>
    public static Result<OrderLine> Create(OrderLineId id, ProductId productId, string productName, Money unitPrice, int quantity) {
        try {
            return Result<OrderLine>.Success(new OrderLine(id, productId, productName, unitPrice, quantity));
        }
        catch (Exception ex) {
            return Result<OrderLine>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Calcula el subtotal de esta línea (UnitPrice * Quantity).
    /// </summary>
    public Result<Money> GetSubtotal() => UnitPrice.Multiply(Quantity);

    /// <summary>
    /// Actualiza la cantidad de la línea. Devuelve Failure si la cantidad es inválida.
    /// </summary>
    public Result UpdateQuantity(int newQuantity) {
        if (newQuantity <= 0)
            return Result.Failure(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(ProductId.Value));

        Quantity = newQuantity;
        return Result.Success();
    }

    public override bool Equals(object? obj) {
        if (obj is not OrderLine other)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
