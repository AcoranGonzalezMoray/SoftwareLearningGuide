namespace SoftwareLearningGuide.Core.Business.Aggregates;

using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

/// <summary>
/// Agregado Order (Raíz del Agregado).
/// Representa un pedido completo en el dominio de e-commerce.
/// Encapsula y protege todas las reglas de negocio relacionadas con los pedidos.
/// </summary>
public class Order : ProduceEvents {
    private const int MaxProductsPerLine = 10;
    private readonly List<OrderLine> _lines = new();

    public OrderId Id { get; }
    public CustomerId CustomerId { get; }
    public Address ShippingAddress { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public DateTime CreatedAt { get; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private Order() { }

    private Order(OrderId id, CustomerId customerId, Address shippingAddress) {
        if (id == null)
            throw new ArgumentNullException(nameof(id));
        if (customerId == null)
            throw new ArgumentNullException(nameof(customerId));
        if (shippingAddress == null)
            throw new ArgumentNullException(nameof(shippingAddress));

        Id = id;
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;

        var totalAmount = GetTotalAmount();
        AddDomainEvent(new OrderCreatedDomainEvent {
            OrderId = id.Value,
            CustomerId = customerId.Value,
            TotalAmount = totalAmount.IsSuccess ? totalAmount.Value!.Amount : 0m,
            Currency = totalAmount.IsSuccess ? totalAmount.Value!.Currency : "USD",
            CreatedAt = CreatedAt
        });
    }

    public static Result<Order> Create(OrderId id, CustomerId customerId, Address shippingAddress) {
        try {
            return Result<Order>.Success(new Order(id, customerId, shippingAddress));
        }
        catch (Exception ex) {
            return Result<Order>.Failure(ex.Message);
        }
    }

    #region Helpers de Validación

    private Result EnsurePending() {
        if (Status != OrderStatus.Pending)
            return Result.Failure(DomainErrors.Order.RequiresPendingState(Status.ToString()));
        return Result.Success();
    }

    private Result ValidateProductForOrder(Product product, int quantity) {
        if (product == null)
            return Result.Failure(DomainErrors.Order.ProductCannotBeNull());

        if (quantity <= 0)
            return Result.Failure(DomainErrors.Order.QuantityMustBeGreaterThanZero(product.Id.Value));

        if (quantity > MaxProductsPerLine)
            return Result.Failure(
                DomainErrors.Order.ExceedsMaxQuantityPerProduct(product.Id.Value, MaxProductsPerLine));

        var pendingResult = EnsurePending();
        if (!pendingResult.IsSuccess)
            return pendingResult;

        if (!product.HasSufficientStock(quantity))
            return Result.Failure(
                DomainErrors.Product.InsufficientStock(product.Name, product.StockQuantity, quantity));

        return Result.Success();
    }

    private Result ValidateQuantityWithinLimit(int quantity) {
        if (quantity > MaxProductsPerLine)
            return Result.Failure(
                DomainErrors.Order.ExceedsMaxQuantityPerProduct(MaxProductsPerLine));
        return Result.Success();
    }

    private OrderLine? FindLine(ProductId productId)
        => _lines.FirstOrDefault(l => l.ProductId.Equals(productId));

    #endregion

    #region Métodos de Consulta

    public bool IsEmpty() => _lines.Count == 0;

    public int GetLineCount() => _lines.Count;

    public int GetTotalItems() => _lines.Sum(line => line.Quantity);

    public bool HasProduct(ProductId productId) => FindLine(productId) != null;

    public int GetProductQuantity(ProductId productId) => FindLine(productId)?.Quantity ?? 0;

    public Result<Money> GetTotalAmount() {
        if (_lines.Count == 0)
            return Result<Money>.Success(Money.Zero());

        var firstLineResult = _lines.First().GetSubtotal();
        if (!firstLineResult.IsSuccess)
            return firstLineResult;

        Money totalAmount = firstLineResult.Value!;
        for (int i = 1; i < _lines.Count; i++) {
            var subtotalResult = _lines[i].GetSubtotal();
            if (!subtotalResult.IsSuccess)
                return Result<Money>.Failure(
                    DomainErrors.Order.ErrorCalculatingSubtotal(i, subtotalResult.Error!));

            var addResult = totalAmount.Add(subtotalResult.Value!);
            if (!addResult.IsSuccess)
                return addResult;

            totalAmount = addResult.Value!;
        }

        return Result<Money>.Success(totalAmount);
    }

    /// <summary>
    /// Calcula el precio promedio por línea del pedido usando Money.Divide.
    /// </summary>
    public Result<Money> GetAverageLinePrice() {
        if (_lines.Count == 0)
            return Result<Money>.Failure(DomainErrors.Order.NoLinesToCalculateAverage());

        var totalResult = GetTotalAmount();
        if (!totalResult.IsSuccess)
            return totalResult;

        return totalResult.Value!.Divide(_lines.Count);
    }

    /// <summary>
    /// Obtiene la línea con mayor precio unitario usando Money.IsGreaterThan.
    /// </summary>
    public Result<OrderLine?> GetMostExpensiveLine() {
        if (_lines.Count == 0)
            return Result<OrderLine?>.Success(null);

        OrderLine mostExpensive = _lines.First();
        foreach (var line in _lines.Skip(1)) {
            var comparison = line.UnitPrice.IsGreaterThan(mostExpensive.UnitPrice);
            if (comparison.IsSuccess && comparison.Value!)
                mostExpensive = line;
        }

        return Result<OrderLine?>.Success(mostExpensive);
    }

    /// <summary>
    /// Obtiene la línea con menor precio unitario usando Money.IsLessThan.
    /// </summary>
    public Result<OrderLine?> GetCheapestLine() {
        if (_lines.Count == 0)
            return Result<OrderLine?>.Success(null);

        OrderLine cheapest = _lines.First();
        foreach (var line in _lines.Skip(1)) {
            var comparison = line.UnitPrice.IsLessThan(cheapest.UnitPrice);
            if (comparison.IsSuccess && comparison.Value!)
                cheapest = line;
        }

        return Result<OrderLine?>.Success(cheapest);
    }

    /// <summary>
    /// Verifica si alguna línea tiene precio unitario mayor al umbral usando Money.IsGreaterThan.
    /// </summary>
    public Result<bool> HasItemsAbovePrice(Money threshold) {
        if (threshold == null)
            return Result<bool>.Failure(DomainErrors.Order.PriceThresholdCannotBeNull());

        foreach (var line in _lines) {
            var comparison = line.UnitPrice.IsGreaterThan(threshold);
            if (comparison.IsSuccess && comparison.Value!)
                return Result<bool>.Success(true);
        }

        return Result<bool>.Success(false);
    }

    /// <summary>
    /// Verifica si el total del pedido alcanza el mínimo requerido usando Money.IsGreaterThanOrEqual.
    /// Útil para reglas como "envío gratuito a partir de $X".
    /// </summary>
    public Result<bool> MeetsMinimumOrderValue(Money minimum) {
        if (minimum == null)
            return Result<bool>.Failure(DomainErrors.Order.MinimumOrderValueCannotBeNull());

        var totalResult = GetTotalAmount();
        if (!totalResult.IsSuccess)
            return Result<bool>.Failure(totalResult.Error!);

        return totalResult.Value!.IsGreaterThanOrEqual(minimum);
    }

    /// <summary>
    /// Calcula el monto a reembolsar para un producto y cantidad específicos usando Money.Multiply.
    /// </summary>
    public Result<Money> CalculateRefundForProduct(ProductId productId, int quantity) {
        if (productId == null)
            return Result<Money>.Failure(DomainErrors.Order.ProductIdCannotBeNull());

        if (quantity <= 0)
            return Result<Money>.Failure(DomainErrors.Order.RefundQuantityMustBeGreaterThanZero(productId.Value));

        var line = FindLine(productId);
        if (line == null)
            return Result<Money>.Failure(DomainErrors.Order.ProductNotFoundInOrder(productId.Value));

        if (quantity > line.Quantity)
            return Result<Money>.Failure(
                DomainErrors.Order.RefundExceedsAvailable(productId.Value, line.Quantity));

        return line.UnitPrice.Multiply(quantity);
    }

    /// <summary>
    /// Calcula la diferencia entre el total del pedido y otro monto usando Money.Subtract.
    /// Útil para comparar precios o calcular diferencias entre pedidos.
    /// </summary>
    public Result<Money> CalculateTotalDifference(Money otherTotal) {
        if (otherTotal == null)
            return Result<Money>.Failure(DomainErrors.Order.OtherTotalCannotBeNull());

        var totalResult = GetTotalAmount();
        if (!totalResult.IsSuccess)
            return totalResult;

        return totalResult.Value!.Subtract(otherTotal);
    }

    /// <summary>
    /// Verifica si dos líneas tienen el mismo precio unitario usando Money.IsEqual.
    /// </summary>
    public Result<bool> HasEqualPriceLines(ProductId productId1, ProductId productId2) {
        if (productId1 == null)
            return Result<bool>.Failure(DomainErrors.Order.ProductId1CannotBeNull());
        if (productId2 == null)
            return Result<bool>.Failure(DomainErrors.Order.ProductId2CannotBeNull());

        var line1 = FindLine(productId1);
        var line2 = FindLine(productId2);

        if (line1 == null)
            return Result<bool>.Failure(DomainErrors.Order.ProductNotFoundInOrder(productId1.Value));
        if (line2 == null)
            return Result<bool>.Failure(DomainErrors.Order.ProductNotFoundInOrder(productId2.Value));

        return line1.UnitPrice.IsEqual(line2.UnitPrice)
            ? Result<bool>.Success(true)
            : Result<bool>.Success(false);
    }

    #endregion

    #region Métodos de Comando

    /// <summary>
    /// Añade un producto al pedido o incrementa su cantidad si ya existe.
    /// </summary>
    public Result AddProduct(Product product, int quantity) {
        var validationResult = ValidateProductForOrder(product, quantity);
        if (!validationResult.IsSuccess)
            return validationResult;

        var existingLine = FindLine(product!.Id);
        if (existingLine != null) {
            var newQuantity = existingLine.Quantity + quantity;
            var limitResult = ValidateQuantityWithinLimit(newQuantity);
            if (!limitResult.IsSuccess)
                return limitResult;

            return existingLine.UpdateQuantity(newQuantity);
        }

        var lineResult = OrderLine.Create(
            OrderLineId.Create(),
            product.Id,
            product.Name,
            product.Price,
            quantity);

        if (!lineResult.IsSuccess)
            return Result.Failure(lineResult.Error!);

        _lines.Add(lineResult.Value!);
        return Result.Success();
    }

    /// <summary>
    /// Remueve un producto del pedido por su ProductId.
    /// </summary>
    public Result RemoveProduct(ProductId productId) {
        if (productId == null)
            return Result.Failure(DomainErrors.Order.ProductIdCannotBeNull());

        var pendingResult = EnsurePending();
        if (!pendingResult.IsSuccess)
            return pendingResult;

        var line = FindLine(productId);
        if (line == null)
            return Result.Failure(DomainErrors.Order.ProductNotFoundInOrder(productId.Value));

        _lines.Remove(line);
        return Result.Success();
    }

    /// <summary>
    /// Actualiza la cantidad de un producto en el pedido.
    /// Si la cantidad es 0, elimina la línea.
    /// </summary>
    public Result UpdateProductQuantity(ProductId productId, int newQuantity) {
        if (productId == null)
            return Result.Failure(DomainErrors.Order.ProductIdCannotBeNull());

        if (newQuantity < 0)
            return Result.Failure(DomainErrors.Order.QuantityCannotBeNegative(productId.Value));

        var pendingResult = EnsurePending();
        if (!pendingResult.IsSuccess)
            return pendingResult;

        var line = FindLine(productId);
        if (line == null)
            return Result.Failure(DomainErrors.Order.ProductNotFoundInOrder(productId.Value));

        if (newQuantity == 0) {
            _lines.Remove(line);
            return Result.Success();
        }

        var limitResult = ValidateQuantityWithinLimit(newQuantity);
        if (!limitResult.IsSuccess)
            return limitResult;

        return line.UpdateQuantity(newQuantity);
    }

    /// <summary>
    /// Limpia todos los productos del pedido.
    /// </summary>
    public Result Clear() {
        var pendingResult = EnsurePending();
        if (!pendingResult.IsSuccess)
            return pendingResult;

        _lines.Clear();
        return Result.Success();
    }

    /// <summary>
    /// Confirma el pedido. Valida que no esté vacío, esté en Pending y el total sea mayor a cero.
    /// </summary>
    public Result Confirm() {
        if (IsEmpty())
            return Result.Failure(DomainErrors.Order.CannotConfirmEmptyOrder());

        var pendingResult = EnsurePending();
        if (!pendingResult.IsSuccess)
            return pendingResult;

        var totalResult = GetTotalAmount();
        if (!totalResult.IsSuccess)
            return Result.Failure(totalResult.Error!);

        var zeroResult = Money.Create(0, totalResult.Value!.Currency);
        if (!zeroResult.IsSuccess)
            return Result.Failure(DomainErrors.Order.TotalAmountValidationFailed());

        var isPositive = totalResult.Value!.IsGreaterThan(zeroResult.Value!);
        if (!isPositive.IsSuccess)
            return Result.Failure(isPositive.Error ?? DomainErrors.Order.TotalAmountMustBeGreaterThanZero());

        if (!isPositive.Value!)
            return Result.Failure(DomainErrors.Order.TotalAmountMustBeGreaterThanZero());

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Marca el pedido como enviado.
    /// </summary>
    public Result Ship() {
        if (Status != OrderStatus.Confirmed)
            return Result.Failure(
                DomainErrors.Order.CannotShipOrderInState(Status.ToString()));

        Status = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Marca el pedido como entregado.
    /// </summary>
    public Result Deliver() {
        if (Status != OrderStatus.Shipped)
            return Result.Failure(
                DomainErrors.Order.CannotDeliverOrderInState(Status.ToString()));

        Status = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Cancela el pedido. Solo puede ser cancelado si está en Pending o Confirmed.
    /// </summary>
    public Result Cancel() {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
            return Result.Failure(
                DomainErrors.Order.CannotCancelOrderInState(Status.ToString()));

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        AddDomainEvent(new OrderCancelledDomainEvent {
            OrderId = Id.Value,
            Reason = "Cancelled by user",
            CancelledAt = CancelledAt.Value
        });

        return Result.Success();
    }

    /// <summary>
    /// Actualiza la dirección de envío del pedido.
    /// </summary>
    public Result UpdateShippingAddress(Address newAddress) {
        if (newAddress == null)
            return Result.Failure(DomainErrors.Order.ShippingAddressCannotBeNull());

        var pendingResult = EnsurePending();
        if (!pendingResult.IsSuccess)
            return pendingResult;

        ShippingAddress = newAddress;
        return Result.Success();
    }

    #endregion

    public override bool Equals(object? obj) {
        if (obj is not Order other)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
