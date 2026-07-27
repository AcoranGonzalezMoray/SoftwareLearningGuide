using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.Aggregates;
using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.Aggregates;

public class OrderShould {
    private static OrderId ValidOrderId() => OrderId.Create();
    private static CustomerId ValidCustomerId() => CustomerId.Create();
    private static Address ValidAddress() => new("123 Main St", "Springfield", "IL", "62704", "US");
    private static Product ValidProduct(int stock = 100) => new(ProductId.Create(), "Laptop", "Gaming", new Money(100m, "USD"), stock);
    private static Order ValidOrder() => new(ValidOrderId(), ValidCustomerId(), ValidAddress());

    #region Create

    [Test]
    public void Create_ValidOrder_ReturnsSuccess() {
        var id = ValidOrderId();
        var customerId = ValidCustomerId();
        var address = ValidAddress();

        var result = Order.Create(id, customerId, address);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
        result.Value!.CustomerId.Should().Be(customerId);
        result.Value!.ShippingAddress.Should().Be(address);
        result.Value!.Status.Should().Be(OrderStatus.Pending);
        result.Value!.CreatedAt.Should().NotBe(default);
    }

    [Test]
    public void Create_EmptyOrderId_ReturnsFailure() {
        var act = () => new Order(new OrderId(Guid.Empty), ValidCustomerId(), ValidAddress());

        act.Should().Throw<ArgumentException>()
            .WithMessage(DomainErrors.IdErrors.OrderIdCannotBeEmpty());
    }

    [Test]
    public void Create_NullOrderId_ThrowsArgumentNullException() {
        var act = () => new Order(null!, ValidCustomerId(), ValidAddress());

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Create_NullCustomerId_ThrowsArgumentNullException() {
        var act = () => new Order(ValidOrderId(), null!, ValidAddress());

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Create_NullAddress_ThrowsArgumentNullException() {
        var act = () => new Order(ValidOrderId(), ValidCustomerId(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Create_FiresOrderCreatedDomainEvent() {
        var order = ValidOrder();

        order.DomainEvents.Should().Contain(e => e is OrderCreatedDomainEvent);
    }

    [Test]
    public void Create_WithNullCustomerId_ReturnsFailure() {
        var result = Order.Create(ValidOrderId(), null!, ValidAddress());

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region IsEmpty / GetLineCount / GetTotalItems / HasProduct / GetProductQuantity

    [Test]
    public void IsEmpty_NewOrder_ReturnsTrue() {
        var order = ValidOrder();

        order.IsEmpty().Should().BeTrue();
    }

    [Test]
    public void IsEmpty_AfterAddingProduct_ReturnsFalse() {
        var order = ValidOrder();
        order.AddProduct(ValidProduct(), 1);

        order.IsEmpty().Should().BeFalse();
    }

    [Test]
    public void GetLineCount_EmptyOrder_ReturnsZero() {
        var order = ValidOrder();

        order.GetLineCount().Should().Be(0);
    }

    [Test]
    public void GetLineCount_WithProducts_ReturnsCorrectCount() {
        var order = ValidOrder();
        order.AddProduct(ValidProduct(), 1);
        order.AddProduct(ValidProduct(), 2);

        order.GetLineCount().Should().Be(2);
    }

    [Test]
    public void GetTotalItems_ReturnsSumOfQuantities() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 3);

        order.GetTotalItems().Should().Be(3);
    }

    [Test]
    public void GetTotalItems_EmptyOrder_ReturnsZero() {
        var order = ValidOrder();

        order.GetTotalItems().Should().Be(0);
    }

    [Test]
    public void HasProduct_ExistingProduct_ReturnsTrue() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);

        order.HasProduct(product.Id).Should().BeTrue();
    }

    [Test]
    public void HasProduct_NonExistingProduct_ReturnsFalse() {
        var order = ValidOrder();

        order.HasProduct(ProductId.Create()).Should().BeFalse();
    }

    [Test]
    public void GetProductQuantity_ExistingProduct_ReturnsQuantity() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 3);

        order.GetProductQuantity(product.Id).Should().Be(3);
    }

    [Test]
    public void GetProductQuantity_NonExistingProduct_ReturnsZero() {
        var order = ValidOrder();

        order.GetProductQuantity(ProductId.Create()).Should().Be(0);
    }

    #endregion

    #region AddProduct

    [Test]
    public void AddProduct_ValidProduct_ReturnsSuccess() {
        var order = ValidOrder();
        var product = ValidProduct();

        var result = order.AddProduct(product, 2);

        result.IsSuccess.Should().BeTrue();
        order.Lines.Should().HaveCount(1);
        order.GetTotalItems().Should().Be(2);
    }

    [Test]
    public void AddProduct_DuplicateProduct_IncreasesQuantity() {
        var order = ValidOrder();
        var product = ValidProduct();

        order.AddProduct(product, 2);
        order.AddProduct(product, 3);

        order.Lines.Should().HaveCount(1);
        order.GetTotalItems().Should().Be(5);
    }

    [Test]
    public void AddProduct_ExceedsMaxQuantity_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();

        var result = order.AddProduct(product, 11);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ExceedsMaxQuantityPerProduct(product.Id.Value, 10));
    }

    [Test]
    public void AddProduct_DuplicateExceedsMaxQuantity_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 6);

        var result = order.AddProduct(product, 6);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ExceedsMaxQuantityPerProduct(10));
    }

    [Test]
    public void AddProduct_InsufficientStock_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct(stock: 2);

        var result = order.AddProduct(product, 5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.InsufficientStock(product.Name, product.StockQuantity, 5));
    }

    [Test]
    public void AddProduct_ZeroQuantity_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();

        var result = order.AddProduct(product, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.QuantityMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void AddProduct_NegativeQuantity_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();

        var result = order.AddProduct(product, -1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void AddProduct_NullProduct_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.AddProduct(null!, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductCannotBeNull());
    }

    [Test]
    public void AddProduct_OnConfirmedOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();

        var result = order.AddProduct(product, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RequiresPendingState(OrderStatus.Confirmed.ToString()));
    }

    #endregion

    #region RemoveProduct

    [Test]
    public void RemoveProduct_ExistingProduct_ReturnsSuccess() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 2);

        var result = order.RemoveProduct(product.Id);

        result.IsSuccess.Should().BeTrue();
        order.Lines.Should().BeEmpty();
    }

    [Test]
    public void RemoveProduct_NonExistingProduct_ReturnsFailure() {
        var order = ValidOrder();
        var productId = ProductId.Create();

        var result = order.RemoveProduct(productId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductNotFoundInOrder(productId.Value));
    }

    [Test]
    public void RemoveProduct_NullProductId_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.RemoveProduct(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductIdCannotBeNull());
    }

    [Test]
    public void RemoveProduct_OnConfirmedOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();

        var result = order.RemoveProduct(product.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RequiresPendingState(OrderStatus.Confirmed.ToString()));
    }

    #endregion

    #region UpdateProductQuantity

    [Test]
    public void UpdateProductQuantity_ValidQuantity_ReturnsSuccess() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 2);

        var result = order.UpdateProductQuantity(product.Id, 5);

        result.IsSuccess.Should().BeTrue();
        order.GetTotalItems().Should().Be(5);
    }

    [Test]
    public void UpdateProductQuantity_ZeroQuantity_RemovesLine() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 2);

        var result = order.UpdateProductQuantity(product.Id, 0);

        result.IsSuccess.Should().BeTrue();
        order.Lines.Should().BeEmpty();
    }

    [Test]
    public void UpdateProductQuantity_NegativeQuantity_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 2);

        var result = order.UpdateProductQuantity(product.Id, -1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.QuantityCannotBeNegative(product.Id.Value));
    }

    [Test]
    public void UpdateProductQuantity_NonExistingProduct_ReturnsFailure() {
        var order = ValidOrder();
        var productId = ProductId.Create();

        var result = order.UpdateProductQuantity(productId, 3);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductNotFoundInOrder(productId.Value));
    }

    [Test]
    public void UpdateProductQuantity_NullProductId_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.UpdateProductQuantity(null!, 3);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductIdCannotBeNull());
    }

    [Test]
    public void UpdateProductQuantity_OnConfirmedOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 2);
        order.Confirm();

        var result = order.UpdateProductQuantity(product.Id, 5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RequiresPendingState(OrderStatus.Confirmed.ToString()));
    }

    #endregion

    #region Clear

    [Test]
    public void Clear_WithPendingStatus_RemovesAllLines() {
        var order = ValidOrder();
        var product1 = ValidProduct();
        var product2 = new Product(ProductId.Create(), "Desktop", "Office", new Money(200m, "USD"), 50);
        order.AddProduct(product1, 2);
        order.AddProduct(product2, 1);

        var result = order.Clear();

        result.IsSuccess.Should().BeTrue();
        order.Lines.Should().BeEmpty();
    }

    [Test]
    public void Clear_OnConfirmedOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();

        var result = order.Clear();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RequiresPendingState(OrderStatus.Confirmed.ToString()));
    }

    #endregion

    #region Confirm

    [Test]
    public void Confirm_WithProducts_ReturnsSuccess() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 2);

        var result = order.Confirm();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();
    }

    [Test]
    public void Confirm_EmptyOrder_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.Confirm();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.CannotConfirmEmptyOrder());
    }

    [Test]
    public void Confirm_AlreadyConfirmed_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();

        var result = order.Confirm();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RequiresPendingState(OrderStatus.Confirmed.ToString()));
    }

    #endregion

    #region Ship

    [Test]
    public void Ship_ConfirmedOrder_ReturnsSuccess() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();

        var result = order.Ship();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().NotBeNull();
    }

    [Test]
    public void Ship_PendingOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);

        var result = order.Ship();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.CannotShipOrderInState(OrderStatus.Pending.ToString()));
    }

    [Test]
    public void Ship_DeliveredOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();
        order.Ship();
        order.Deliver();

        var result = order.Ship();

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Deliver

    [Test]
    public void Deliver_ShippedOrder_ReturnsSuccess() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();
        order.Ship();

        var result = order.Deliver();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
    }

    [Test]
    public void Deliver_ConfirmedOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();

        var result = order.Deliver();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.CannotDeliverOrderInState(OrderStatus.Confirmed.ToString()));
    }

    [Test]
    public void Deliver_PendingOrder_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.Deliver();

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Cancel

    [Test]
    public void Cancel_PendingOrder_ReturnsSuccess() {
        var order = ValidOrder();

        var result = order.Cancel();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
    }

    [Test]
    public void Cancel_ConfirmedOrder_ReturnsSuccess() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();

        var result = order.Cancel();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Test]
    public void Cancel_ShippedOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();
        order.Ship();

        var result = order.Cancel();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.CannotCancelOrderInState(OrderStatus.Shipped.ToString()));
    }

    [Test]
    public void Cancel_DeliveredOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();
        order.Ship();
        order.Deliver();

        var result = order.Cancel();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.CannotCancelOrderInState(OrderStatus.Delivered.ToString()));
    }

    [Test]
    public void Cancel_AlreadyCancelled_ReturnsFailure() {
        var order = ValidOrder();
        order.Cancel();

        var result = order.Cancel();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.CannotCancelOrderInState(OrderStatus.Cancelled.ToString()));
    }

    [Test]
    public void Cancel_FiresOrderCancelledDomainEvent() {
        var order = ValidOrder();

        order.Cancel();

        order.DomainEvents.Should().Contain(e => e is OrderCancelledDomainEvent);
    }

    #endregion

    #region GetTotalAmount

    [Test]
    public void GetTotalAmount_EmptyOrder_ReturnsZero() {
        var order = ValidOrder();

        var result = order.GetTotalAmount();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(0m);
        result.Value!.Currency.Should().Be("USD");
    }

    [Test]
    public void GetTotalAmount_WithProducts_ReturnsCorrectTotal() {
        var order = ValidOrder();
        var product1 = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        var product2 = new Product(ProductId.Create(), "B", "Desc", new Money(30m, "USD"), 100);
        order.AddProduct(product1, 2);
        order.AddProduct(product2, 3);

        var result = order.GetTotalAmount();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(190m);
    }

    [Test]
    public void GetTotalAmount_WithSingleProduct_ReturnsUnitPriceTimesQuantity() {
        var order = ValidOrder();
        order.AddProduct(ValidProduct(), 3);

        var result = order.GetTotalAmount();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(300m);
    }

    #endregion

    #region GetAverageLinePrice

    [Test]
    public void GetAverageLinePrice_WithProducts_ReturnsCorrectAverage() {
        var order = ValidOrder();
        var product1 = new Product(ProductId.Create(), "A", "Desc", new Money(100m, "USD"), 100);
        var product2 = new Product(ProductId.Create(), "B", "Desc", new Money(200m, "USD"), 100);
        order.AddProduct(product1, 1);
        order.AddProduct(product2, 1);

        var result = order.GetAverageLinePrice();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(150m);
    }

    [Test]
    public void GetAverageLinePrice_EmptyOrder_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.GetAverageLinePrice();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.NoLinesToCalculateAverage());
    }

    [Test]
    public void GetAverageLinePrice_WithSingleLine_ReturnsLineAverage() {
        var order = ValidOrder();
        order.AddProduct(ValidProduct(), 2);

        var result = order.GetAverageLinePrice();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(200m);
    }

    #endregion

    #region GetMostExpensiveLine / GetCheapestLine

    [Test]
    public void GetMostExpensiveLine_WithProducts_ReturnsCorrectLine() {
        var order = ValidOrder();
        var cheap = new Product(ProductId.Create(), "Cheap", "Desc", new Money(10m, "USD"), 100);
        var expensive = new Product(ProductId.Create(), "Expensive", "Desc", new Money(100m, "USD"), 100);
        order.AddProduct(cheap, 1);
        order.AddProduct(expensive, 1);

        var result = order.GetMostExpensiveLine();

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductName.Should().Be("Expensive");
    }

    [Test]
    public void GetMostExpensiveLine_EmptyOrder_ThrowsArgumentNullException() {
        var order = ValidOrder();

        var act = () => order.GetMostExpensiveLine();

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void GetMostExpensiveLine_WithSingleLine_ReturnsThatLine() {
        var order = ValidOrder();
        order.AddProduct(ValidProduct(), 1);

        var result = order.GetMostExpensiveLine();

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductName.Should().Be("Laptop");
    }

    [Test]
    public void GetCheapestLine_WithProducts_ReturnsCorrectLine() {
        var order = ValidOrder();
        var cheap = new Product(ProductId.Create(), "Cheap", "Desc", new Money(10m, "USD"), 100);
        var expensive = new Product(ProductId.Create(), "Expensive", "Desc", new Money(100m, "USD"), 100);
        order.AddProduct(cheap, 1);
        order.AddProduct(expensive, 1);

        var result = order.GetCheapestLine();

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductName.Should().Be("Cheap");
    }

    [Test]
    public void GetCheapestLine_EmptyOrder_ThrowsArgumentNullException() {
        var order = ValidOrder();

        var act = () => order.GetCheapestLine();

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void GetCheapestLine_WithSingleLine_ReturnsThatLine() {
        var order = ValidOrder();
        order.AddProduct(ValidProduct(), 1);

        var result = order.GetCheapestLine();

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductName.Should().Be("Laptop");
    }

    #endregion

    #region HasItemsAbovePrice

    [Test]
    public void HasItemsAbovePrice_AboveThreshold_ReturnsTrue() {
        var order = ValidOrder();
        var cheap = new Product(ProductId.Create(), "Cheap", "Desc", new Money(10m, "USD"), 100);
        var expensive = new Product(ProductId.Create(), "Expensive", "Desc", new Money(100m, "USD"), 100);
        order.AddProduct(cheap, 1);
        order.AddProduct(expensive, 1);

        var result = order.HasItemsAbovePrice(new Money(50m, "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void HasItemsAbovePrice_BelowThreshold_ReturnsFalse() {
        var order = ValidOrder();
        var cheap = new Product(ProductId.Create(), "Cheap", "Desc", new Money(10m, "USD"), 100);
        order.AddProduct(cheap, 1);

        var result = order.HasItemsAbovePrice(new Money(50m, "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void HasItemsAbovePrice_EmptyOrder_ReturnsFalse() {
        var order = ValidOrder();

        var result = order.HasItemsAbovePrice(new Money(50m, "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void HasItemsAbovePrice_NullThreshold_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.HasItemsAbovePrice(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.PriceThresholdCannotBeNull());
    }

    #endregion

    #region MeetsMinimumOrderValue

    [Test]
    public void MeetsMinimumOrderValue_AboveMinimum_ReturnsTrue() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(100m, "USD"), 100);
        order.AddProduct(product, 2);

        var result = order.MeetsMinimumOrderValue(new Money(150m, "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void MeetsMinimumOrderValue_EqualMinimum_ReturnsTrue() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(100m, "USD"), 100);
        order.AddProduct(product, 2);

        var result = order.MeetsMinimumOrderValue(new Money(200m, "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void MeetsMinimumOrderValue_BelowMinimum_ReturnsFalse() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(100m, "USD"), 100);
        order.AddProduct(product, 1);

        var result = order.MeetsMinimumOrderValue(new Money(200m, "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void MeetsMinimumOrderValue_NullMinimum_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.MeetsMinimumOrderValue(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.MinimumOrderValueCannotBeNull());
    }

    [Test]
    public void MeetsMinimumOrderValue_EmptyOrder_ReturnsFalse() {
        var order = ValidOrder();

        var result = order.MeetsMinimumOrderValue(new Money(100m, "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    #endregion

    #region CalculateRefundForProduct

    [Test]
    public void CalculateRefundForProduct_ValidProduct_ReturnsCorrectAmount() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        order.AddProduct(product, 5);

        var result = order.CalculateRefundForProduct(product.Id, 3);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(150m);
    }

    [Test]
    public void CalculateRefundForProduct_SingleUnit_ReturnsUnitPrice() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        order.AddProduct(product, 5);

        var result = order.CalculateRefundForProduct(product.Id, 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(50m);
    }

    [Test]
    public void CalculateRefundForProduct_NullProductId_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.CalculateRefundForProduct(null!, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductIdCannotBeNull());
    }

    [Test]
    public void CalculateRefundForProduct_ZeroQuantity_ReturnsFailure() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        order.AddProduct(product, 5);

        var result = order.CalculateRefundForProduct(product.Id, 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RefundQuantityMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void CalculateRefundForProduct_NegativeQuantity_ReturnsFailure() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        order.AddProduct(product, 5);

        var result = order.CalculateRefundForProduct(product.Id, -1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RefundQuantityMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void CalculateRefundForProduct_ProductNotFound_ReturnsFailure() {
        var order = ValidOrder();
        var productId = ProductId.Create();

        var result = order.CalculateRefundForProduct(productId, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductNotFoundInOrder(productId.Value));
    }

    [Test]
    public void CalculateRefundForProduct_ExceedsAvailable_ReturnsFailure() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        order.AddProduct(product, 3);

        var result = order.CalculateRefundForProduct(product.Id, 5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RefundExceedsAvailable(product.Id.Value, 3));
    }

    #endregion

    #region CalculateTotalDifference

    [Test]
    public void CalculateTotalDifference_ValidOtherTotal_ReturnsDifference() {
        var order = ValidOrder();
        var product = new Product(ProductId.Create(), "A", "Desc", new Money(100m, "USD"), 100);
        order.AddProduct(product, 3);

        var result = order.CalculateTotalDifference(new Money(100m, "USD"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(200m);
    }

    [Test]
    public void CalculateTotalDifference_EmptyOrder_ReturnsNegative() {
        var order = ValidOrder();

        var result = order.CalculateTotalDifference(new Money(100m, "USD"));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void CalculateTotalDifference_NullOtherTotal_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.CalculateTotalDifference(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.OtherTotalCannotBeNull());
    }

    #endregion

    #region HasEqualPriceLines

    [Test]
    public void HasEqualPriceLines_SamePrice_ReturnsTrue() {
        var order = ValidOrder();
        var product1 = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        var product2 = new Product(ProductId.Create(), "B", "Desc", new Money(50m, "USD"), 100);
        order.AddProduct(product1, 1);
        order.AddProduct(product2, 1);

        var result = order.HasEqualPriceLines(product1.Id, product2.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void HasEqualPriceLines_DifferentPrice_ReturnsFalse() {
        var order = ValidOrder();
        var product1 = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        var product2 = new Product(ProductId.Create(), "B", "Desc", new Money(100m, "USD"), 100);
        order.AddProduct(product1, 1);
        order.AddProduct(product2, 1);

        var result = order.HasEqualPriceLines(product1.Id, product2.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void HasEqualPriceLines_NullProductId1_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.HasEqualPriceLines(null!, ProductId.Create());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductId1CannotBeNull());
    }

    [Test]
    public void HasEqualPriceLines_NullProductId2_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.HasEqualPriceLines(ProductId.Create(), null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ProductId2CannotBeNull());
    }

    [Test]
    public void HasEqualPriceLines_Product1NotFound_ReturnsFailure() {
        var order = ValidOrder();
        var product2 = new Product(ProductId.Create(), "B", "Desc", new Money(50m, "USD"), 100);
        order.AddProduct(product2, 1);

        var result = order.HasEqualPriceLines(ProductId.Create(), product2.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no existe");
    }

    [Test]
    public void HasEqualPriceLines_Product2NotFound_ReturnsFailure() {
        var order = ValidOrder();
        var product1 = new Product(ProductId.Create(), "A", "Desc", new Money(50m, "USD"), 100);
        order.AddProduct(product1, 1);

        var result = order.HasEqualPriceLines(product1.Id, ProductId.Create());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no existe");
    }

    #endregion

    #region UpdateShippingAddress

    [Test]
    public void UpdateShippingAddress_PendingOrder_ReturnsSuccess() {
        var order = ValidOrder();
        var newAddress = new Address("456 Oak Ave", "Chicago", "IL", "60601", "US");

        var result = order.UpdateShippingAddress(newAddress);

        result.IsSuccess.Should().BeTrue();
        order.ShippingAddress.Should().Be(newAddress);
    }

    [Test]
    public void UpdateShippingAddress_ConfirmedOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();

        var result = order.UpdateShippingAddress(new Address("456 Oak Ave", "Chicago", "IL", "60601", "US"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.RequiresPendingState(OrderStatus.Confirmed.ToString()));
    }

    [Test]
    public void UpdateShippingAddress_NullAddress_ReturnsFailure() {
        var order = ValidOrder();

        var result = order.UpdateShippingAddress(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.ShippingAddressCannotBeNull());
    }

    [Test]
    public void UpdateShippingAddress_ShippedOrder_ReturnsFailure() {
        var order = ValidOrder();
        var product = ValidProduct();
        order.AddProduct(product, 1);
        order.Confirm();
        order.Ship();

        var result = order.UpdateShippingAddress(new Address("456 Oak Ave", "Chicago", "IL", "60601", "US"));

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Equals

    [Test]
    public void Equals_SameId_ReturnsTrue() {
        var id = ValidOrderId();
        var a = new Order(id, ValidCustomerId(), ValidAddress());
        var b = new Order(id, ValidCustomerId(), ValidAddress());

        a.Equals(b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentId_ReturnsFalse() {
        var a = ValidOrder();
        var b = ValidOrder();

        a.Equals(b).Should().BeFalse();
    }

    [Test]
    public void Equals_NullObject_ReturnsFalse() {
        var order = ValidOrder();

        order.Equals(null).Should().BeFalse();
    }

    [Test]
    public void Equals_DifferentType_ReturnsFalse() {
        var order = ValidOrder();

        order.Equals("not an order").Should().BeFalse();
    }

    #endregion
}
