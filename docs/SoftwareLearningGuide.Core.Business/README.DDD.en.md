<div align="center">

# Domain-Driven Design (DDD)

**Building business logic that reflects how the organization truly works**

[![C#](https://img.shields.io/badge/C%23-13.0-512BD4?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Status](https://img.shields.io/badge/Status-Complete-brightgreen)](#)

</div>

---

#### Table of Contents

- [What is DDD?](#what-is-ddd)
- [Project Structure](#project-structure)
- [Value Objects](#value-objects)
- [Entities](#entities)
- [Aggregates](#aggregates)
- [Domain Events](#domain-events)
- [Comparisons and Differences](#comparisons-and-differences)
- [Project Examples](#project-examples)
- [Benefits of DDD in This Project](#benefits-of-ddd-in-this-project)
- [Recommended Resources](#recommended-resources)

---

## What is DDD?

**Domain-Driven Design** is a software development approach that focuses on creating a deep understanding of the business domain and reflecting that understanding in the code. Instead of thinking in terms of databases and frameworks, we think in terms of **business concepts**.

**Core idea:** The code should speak the same language as the business. If the business talks about "Orders", "Products", and "Customers", so should the code.

**The 3 building blocks of DDD in this project:**

| Building Block | Purpose | Example |
|---|---|---|
| **Value Objects** | Represent concepts without identity | `Money`, `Email`, `Address` |
| **Entities** | Represent objects with identity that change | `Product`, `Customer`, `Order` |
| **Aggregates** | Groups of entities that must be consistent | `Order` (with its `OrderLines`) |

> **Why are constructors in DDD private (`private`)?**
>
> 1. **Domain Invariant Protection:** In DDD, a domain object (Entity, Aggregate, or Value Object) must never exist in an invalid or inconsistent state. If constructors were `public`, external code could instantiate objects using `new MyEntity(...)`, bypassing business rules and domain validations.
> 2. **Encapsulation & Factory Methods (`Create`):** By making constructors `private`, all instantiations are forced through static factory methods (such as `Result<Money>.Create(...)` or `Result<Order>.Create(...)`). These methods validate all domain rules and return a `Result<T>`, preventing object creation whenever a validation fails.
> 3. **ORM Compatibility (EF Core):** EF Core requires a parameterless constructor to reconstruct entities from the database via reflection. This constructor is also declared `private` (or `protected`), preserving encapsulation throughout the rest of the application.

---

## Project Structure

```
SoftwareLearningGuide.Core.Business/
├── Entities/              # Entities: Product, Customer, OrderLine
├── Aggregates/            # Aggregate Roots: Order
├── ValueObjects/          # Value Objects: Money, Email, Address, etc.
├── Enums/                 # OrderStatus, PaymentMethod, etc.
├── Errors/                # DomainErrors: centralized error catalog
├── Exceptions/            # DomainException for invariant violations
├── DomainEvents/          # ProduceEvents base + event interfaces
└── Helpers/               # Financial rounding helper
```

---

## Value Objects

> **Productive file:** [`SoftwareLearningGuide.Core.Business/ValueObjects/Money.cs`](SoftwareLearningGuide.Core.Business/ValueObjects/Money.cs)

Value Objects are objects that have **no identity** — they are compared by their **value**, not by an ID. Two `Money` objects with the same amount and currency are considered equal.

```csharp
namespace SoftwareLearningGuide.Core.Business.ValueObjects;

using System.Globalization;
using SoftwareLearningGuide.Core.Business.Helpers;

/// <summary>
/// Value Object that represents a monetary amount with its currency.
/// Implements Round Half Up (commercial) rounding for financial operations.
/// </summary>
public class Money {
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency) {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty.", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));

        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);

    public Result<Money> Add(Money other) {
        if (other == null)
            return Result<Money>.Failure("Cannot add null Money.");

        if (Currency != other.Currency)
            return Result<Money>.Failure($"Cannot add Money in {other.Currency} to Money in {Currency}. Different currencies.");

        var result = FinancialRoundingHelper.RoundHalfUp(Amount + other.Amount);
        return Result<Money>.Success(new Money(result, Currency));
    }

    public Result<Money> Subtract(Money other) {
        if (other == null)
            return Result<Money>.Failure("Cannot subtract null Money.");

        if (Currency != other.Currency)
            return Result<Money>.Failure($"Cannot subtract Money in {other.Currency} from Money in {Currency}. Different currencies.");

        if (Amount < other.Amount)
            return Result<Money>.Failure("Insufficient funds. Cannot subtract more than the available amount.");

        var result = FinancialRoundingHelper.RoundHalfUp(Amount - other.Amount);
        return Result<Money>.Success(new Money(result, Currency));
    }

    public Result<Money> Multiply(int quantity) {
        if (quantity < 0)
            return Result<Money>.Failure("Quantity cannot be negative.");

        var result = FinancialRoundingHelper.RoundHalfUp(Amount * quantity);
        return Result<Money>.Success(new Money(result, Currency));
    }

    public Result<bool> IsGreaterThan(Money other) {
        if (other == null)
            return Result<bool>.Failure("Cannot compare with null Money.");

        if (Currency != other.Currency)
            return Result<bool>.Failure("Cannot compare Money with different currencies.");

        return Result<bool>.Success(Amount > other.Amount);
    }

    public Result<bool> IsGreaterThanOrEqual(Money other) {
        if (other == null)
            return Result<bool>.Failure("Cannot compare with null Money.");

        if (Currency != other.Currency)
            return Result<bool>.Failure("Cannot compare Money with different currencies.");

        return Result<bool>.Success(Amount >= other.Amount);
    }

    public Result<bool> IsLessThan(Money other) {
        if (other == null)
            return Result<bool>.Failure("Cannot compare with null Money.");

        if (Currency != other.Currency)
            return Result<bool>.Failure("Cannot compare Money with different currencies.");

        return Result<bool>.Success(Amount < other.Amount);
    }

    public Result<bool> IsLessThanOrEqual(Money other) {
        if (other == null)
            return Result<bool>.Failure("Cannot compare with null Money.");

        if (Currency != other.Currency)
            return Result<bool>.Failure("Cannot compare Money with different currencies.");

        return Result<bool>.Success(Amount <= other.Amount);
    }

    public Result<bool> IsEqual(Money other) {
        if (other == null)
            return Result<bool>.Failure("Cannot compare with null Money.");

        if (Currency != other.Currency)
            return Result<bool>.Failure("Cannot compare Money with different currencies.");

        return Result<bool>.Success(Amount == other.Amount);
    }

    public override bool Equals(object? obj) {
        if (obj is not Money other)
            return false;

        return Amount == other.Amount && Currency == other.Currency;
    }

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount.ToString("N2", new CultureInfo("en-US"))} {Currency}";
}
```

**Key points:**

- **Constructor validates** — it's impossible to create `Money` with invalid data
- **Immutable** — once created, it can't be changed. Operations return new `Money` objects
- **`Result<Money>`** — arithmetic operations return `Result` to handle errors like currency mismatch
- **`FinancialRoundingHelper`** — all amounts use **commercial rounding (Round Half Up)** for financial precision
- **Equality by value** — `Equals` compares amount AND currency
- **Factory method** — `Money.Zero("USD")` for creating zero amounts

### Other Value Objects

> **Productive file:** [`SoftwareLearningGuide.Core.Business/ValueObjects/`](SoftwareLearningGuide.Core.Business/ValueObjects/)

| Value Object | Purpose | File |
|---|---|---|
| `Email` | Validates email format | `Email.cs` |
| `Address` | Shipping/billing address | `Address.cs` |
| `OrderId` | Strongly-typed order identifier | `OrderId.cs` |
| `ProductId` | Strongly-typed product identifier | `ProductId.cs` |
| `CustomerId` | Strongly-typed customer identifier | `CustomerId.cs` |
| `OrderLineId` | Strongly-typed order line identifier | `OrderLineId.cs` |

---

## Entities

> **Productive file:** [`SoftwareLearningGuide.Core.Business/Entities/Product.cs`](SoftwareLearningGuide.Core.Business/Entities/Product.cs)

Entities are objects that have **identity** — they are compared by their **ID**, not by their attributes. A `Product` is still the same product even if you change its name or price.

```csharp
namespace SoftwareLearningGuide.Core.Business.Entities;

using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

public class Product : ProduceEvents {
    public ProductId Id { get; }
    public string Name { get; }
    public string Description { get; }
    public Money Price { get; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    private Product() { }

    public Product(ProductId id, string name, string description, Money price, int stockQuantity) {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(DomainErrors.Product.NameCannotBeEmpty());

        if (name.Length > 200)
            throw new ArgumentException(DomainErrors.Product.NameTooLong(name.Length));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(DomainErrors.Product.DescriptionCannotBeEmpty());

        if (description.Length > 2000)
            throw new ArgumentException(DomainErrors.Product.DescriptionTooLong(description.Length));

        if (price == null)
            throw new ArgumentNullException(nameof(price));

        if (price.Amount <= 0)
            throw new ArgumentException(DomainErrors.Product.PriceMustBePositive());

        if (stockQuantity < 0)
            throw new ArgumentException(DomainErrors.Product.StockCannotBeNegative());

        Id = id;
        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        StockQuantity = stockQuantity;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Product> Create(ProductId id, string name, string description, Money price, int stockQuantity) {
        try {
            var product = new Product(id, name, description, price, stockQuantity);
            return Result<Product>.Success(product);
        }
        catch (Exception ex) {
            return Result<Product>.Failure(ex.Message);
        }
    }

    public Result UpdatePrice(Money newPrice) {
        if (newPrice == null)
            return Result.Failure(DomainErrors.Product.PriceCannotBeNull());

        if (newPrice.Amount <= 0)
            return Result.Failure(DomainErrors.Product.PriceMustBePositive());

        if (newPrice.Currency != Price.Currency)
            return Result.Failure(DomainErrors.Product.PriceCurrencyMismatch(newPrice.Currency, Price.Currency));

        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddStock(int quantity) {
        if (quantity <= 0)
            return Result.Failure(DomainErrors.Product.QuantityMustBePositive());

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RemoveStock(int quantity) {
        if (quantity <= 0)
            return Result.Failure(DomainErrors.Product.QuantityMustBePositive());

        if (StockQuantity < quantity)
            return Result.Failure(DomainErrors.Product.InsufficientStock(StockQuantity, quantity));

        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Deactivate() {
        if (!IsActive)
            return Result.Failure(DomainErrors.Product.ProductAlreadyInactive());

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public override bool Equals(object? obj) {
        if (obj is not Product other)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
```

**Key points:**

- **Identity** — equality is based on `Id`, not attributes
- **Private constructor** — required by EF Core, not used in business logic
- **Factory method** — `Create()` wraps the constructor in a `Result`
- **Encapsulated state** — `StockQuantity` and `IsActive` have `private set`, only changed through methods
- **Validation in constructor** — catches invalid state early
- **`ProduceEvents`** — inherits from the base class for Domain Events support

---

## Aggregates

> **Productive file:** [`SoftwareLearningGuide.Core.Business/Aggregates/Order.cs`](SoftwareLearningGuide.Core.Business/Aggregates/Order.cs)

An Aggregate is a cluster of related entities that is treated as a **single unit** for data changes. The **Aggregate Root** is the only entity outside code can access, and it guarantees the **consistency** of all changes within the Aggregate.

```csharp
namespace SoftwareLearningGuide.Core.Business.Aggregates;

using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Enums;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

public class Order : ProduceEvents {
    public OrderId Id { get; }
    public CustomerId CustomerId { get; }
    public OrderStatus Status { get; private set; }
    public DateTime OrderDate { get; }
    public DateTime? ConfirmationDate { get; private set; }
    public DateTime? ShippingDate { get; private set; }
    public DateTime? DeliveryDate { get; private set; }
    public Address ShippingAddress { get; private set; }
    private readonly List<OrderLine> _orderLines = [];
    public IReadOnlyList<OrderLine> OrderLines => _orderLines.AsReadOnly();

    private Order() { }

    public Order(OrderId id, CustomerId customerId, Address shippingAddress) {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        if (customerId == null)
            throw new ArgumentNullException(nameof(customerId));

        if (shippingAddress == null)
            throw new ArgumentNullException(nameof(shippingAddress));

        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        OrderDate = DateTime.UtcNow;
        ShippingAddress = shippingAddress;

        AddDomainEvent(new OrderCreatedDomainEvent(Id, CustomerId));
    }

    public static Result<Order> Create(OrderId id, CustomerId customerId, Address shippingAddress) {
        try {
            var order = new Order(id, customerId, shippingAddress);
            return Result<Order>.Success(order);
        }
        catch (Exception ex) {
            return Result<Order>.Failure(ex.Message);
        }
    }

    #region Business Rules

    private Result EnsurePending() {
        if (Status != OrderStatus.Pending)
            return Result.Failure(DomainErrors.Order.InvalidStatusTransition(Status, OrderStatus.Pending));
        return Result.Success();
    }

    public Result AddProduct(Product product, int quantity) {
        var pendingResult = EnsurePending();
        if (!pendingResult.IsSuccess)
            return pendingResult;

        if (product == null)
            return Result.Failure(DomainErrors.Order.ProductCannotBeNull());

        if (quantity <= 0)
            return Result.Failure(DomainErrors.Order.QuantityMustBePositive());

        if (quantity > 10)
            return Result.Failure(DomainErrors.Order.MaxQuantityPerProductExceeded(quantity));

        var existingLine = _orderLines.FirstOrDefault(ol => ol.ProductId == product.Id);
        if (existingLine != null) {
            var newQuantity = existingLine.Quantity + quantity;
            if (newQuantity > 10)
                return Result.Failure(DomainErrors.Order.MaxQuantityPerProductExceeded(newQuantity));

            var updateResult = existingLine.UpdateQuantity(newQuantity);
            if (!updateResult.IsSuccess)
                return updateResult;
        }
        else {
            var lineId = OrderLineId.Create();
            var line = new OrderLine(lineId, product.Id, product.Name, product.Price, quantity);
            _orderLines.Add(line);
        }

        return Result.Success();
    }

    public Result Confirm() {
        if (Status != OrderStatus.Pending)
            return Result.Failure(DomainErrors.Order.InvalidStatusTransition(Status, OrderStatus.Pending));

        if (!_orderLines.Any())
            return Result.Failure(DomainErrors.Order CannotBeEmpty());

        Status = OrderStatus.Confirmed;
        ConfirmationDate = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Cancel() {
        if (Status == OrderStatus.Delivered)
            return Result.Failure(DomainErrors.Order.CannotCancelDelivered());

        if (Status == OrderStatus.Cancelled)
            return Result.Failure(DomainErrors.Order.AlreadyCancelled());

        Status = OrderStatus.Cancelled;

        AddDomainEvent(new OrderCancelledDomainEvent(Id, CustomerId));

        return Result.Success();
    }

    public Result Ship() {
        if (Status != OrderStatus.Confirmed)
            return Result.Failure(DomainErrors.Order.InvalidStatusTransition(Status, OrderStatus.Confirmed));

        Status = OrderStatus.Shipped;
        ShippingDate = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Deliver() {
        if (Status != OrderStatus.Shipped)
            return Result.Failure(DomainErrors.Order.InvalidStatusTransition(Status, OrderStatus.Shipped));

        Status = OrderStatus.Delivered;
        DeliveryDate = DateTime.UtcNow;
        return Result.Success();
    }

    public Result<Money> GetTotalAmount() {
        if (!_orderLines.Any())
            return Result<Money>.Failure(DomainErrors.Order.TotalCannotBeCalculatedForEmptyOrder());

        var currency = _orderLines.First().UnitPrice.Currency;
        var total = Money.Zero(currency);

        foreach (var line in _orderLines) {
            var subtotalResult = line.GetSubtotal();
            if (!subtotalResult.IsSuccess)
                return Result<Money>.Failure(subtotalResult.Error);

            var addResult = total.Add(subtotalResult.Value);
            if (!addResult.IsSuccess)
                return Result<Money>.Failure(addResult.Error);

            total = addResult.Value;
        }

        return Result<Money>.Success(total);
    }

    public Result<Money> GetAverageLinePrice() {
        if (!_orderLines.Any())
            return Result<Money>.Failure(DomainErrors.Order.AveragePriceCannotBeCalculatedForEmptyOrder());

        var currency = _orderLines.First().UnitPrice.Currency;
        var total = Money.Zero(currency);

        foreach (var line in _orderLines) {
            var addResult = total.Add(line.UnitPrice);
            if (!addResult.IsSuccess)
                return Result<Money>.Failure(addResult.Error);

            total = addResult.Value;
        }

        var result = FinancialRoundingHelper.RoundHalfUp(total.Amount / _orderLines.Count);
        return Result<Money>.Success(new Money(result, currency));
    }

    public Result<OrderLine?> GetMostExpensiveLine() {
        if (!_orderLines.Any())
            return Result<OrderLine?>.Success(null);

        var mostExpensive = _orderLines.OrderByDescending(ol => ol.UnitPrice.Amount).First();
        return Result<OrderLine?>.Success(mostExpensive);
    }

    public Result<OrderLine?> GetCheapestLine() {
        if (!_orderLines.Any())
            return Result<OrderLine?>.Success(null);

        var cheapest = _orderLines.OrderBy(ol => ol.UnitPrice.Amount).First();
        return Result<OrderLine?>.Success(cheapest);
    }

    public Result<bool> HasItemsAbovePrice(Money price) {
        if (price == null)
            return Result<bool>.Failure("Price to compare cannot be null.");

        foreach (var line in _orderLines) {
            var comparisonResult = line.UnitPrice.IsGreaterThan(price);
            if (!comparisonResult.IsSuccess)
                return Result<bool>.Failure(comparisonResult.Error);

            if (comparisonResult.Value)
                return Result<bool>.Success(true);
        }

        return Result<bool>.Success(false);
    }

    public Result<bool> MeetsMinimumOrderValue(Money minimum) {
        var totalResult = GetTotalAmount();
        if (!totalResult.IsSuccess)
            return Result<bool>.Failure(totalResult.Error);

        return totalResult.Value.IsGreaterThanOrEqual(minimum);
    }

    public Result<Money> CalculateRefundForProduct(ProductId productId, int quantityToRefund) {
        var pendingResult = EnsurePending();
        if (!pendingResult.IsSuccess)
            return Result<Money>.Failure(pendingResult.Error);

        if (quantityToRefund <= 0)
            return Result<Money>.Failure("Quantity to refund must be greater than zero.");

        var orderLine = _orderLines.FirstOrDefault(ol => ol.ProductId == productId);
        if (orderLine == null)
            return Result<Money>.Failure(DomainErrors.Order.ProductNotInOrder(productId.Value));

        if (quantityToRefund > orderLine.Quantity)
            return Result<Money>.Failure(DomainErrors.Order.RefundQuantityExceedsOrderQuantity(quantityToRefund, orderLine.Quantity));

        var refundAmountResult = orderLine.UnitPrice.Multiply(quantityToRefund);
        if (!refundAmountResult.IsSuccess)
            return Result<Money>.Failure(refundAmountResult.Error);

        return refundAmountResult;
    }

    public Result<Money> CalculateTotalDifference(Order otherOrder) {
        if (otherOrder == null)
            return Result<Money>.Failure("Cannot compare with null Order.");

        var thisTotalResult = GetTotalAmount();
        if (!thisTotalResult.IsSuccess)
            return Result<Money>.Failure(thisTotalResult.Error);

        var otherTotalResult = otherOrder.GetTotalAmount();
        if (!otherTotalResult.IsSuccess)
            return Result<Money>.Failure(otherTotalResult.Error);

        var diffResult = thisTotalResult.Value.Subtract(otherTotalResult.Value);
        if (!diffResult.IsSuccess)
            return Result<Money>.Failure(diffResult.Error);

        return diffResult;
    }

    public Result<bool> HasEqualPriceLines() {
        if (!_orderLines.Any())
            return Result<bool>.Success(false);

        var currency = _orderLines.First().UnitPrice.Currency;

        for (int i = 0; i < _orderLines.Count; i++) {
            for (int j = i + 1; j < _orderLines.Count; j++) {
                var comparisonResult = _orderLines[i].UnitPrice.IsEqual(_orderLines[j].UnitPrice);
                if (!comparisonResult.IsSuccess)
                    return Result<bool>.Failure(comparisonResult.Error);

                if (comparisonResult.Value)
                    return Result<bool>.Success(true);
            }
        }

        return Result<bool>.Success(false);
    }

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
```

**Key points:**

- `Order : ProduceEvents` allows it to fire `OrderCreatedDomainEvent` and `OrderCancelledDomainEvent`
- `AddDomainEvent()` is called in the constructor and in `Cancel()`
- Errors use `DomainErrors.Order.*` (centralized catalog)
- `EnsurePending()` is a private helper that validates state before write operations
- `Cancel()` dispatches a domain event that the UnitOfWork later publishes via Outbox
- `AddProduct()` has full implementation: validates the product, checks limits, and creates a new `OrderLine` or increments quantity of an existing one
- There's a `private Order() { }` constructor required by EF Core
- `Create()` is a static factory method that wraps the constructor in a `Result`
- The aggregate exposes many query methods (`GetTotalAmount`, `GetAverageLinePrice`, `GetMostExpensiveLine`, `GetCheapestLine`, `HasItemsAbovePrice`, `MeetsMinimumOrderValue`, `CalculateRefundForProduct`, `CalculateTotalDifference`, `HasEqualPriceLines`) that internally use `Money` comparison methods (`IsGreaterThan`, `IsLessThan`, `IsGreaterThanOrEqual`, `IsEqual`)

**Why is Order an Aggregate and not just an Entity?** Order is an Aggregate because it controls access to its OrderLines. You can't modify an OrderLine directly; you must do it through Order. This ensures that business rules (like "maximum 10 units per product") are always enforced. If OrderLines were accessible directly, someone could add 100 units of a product without Order knowing, breaking business consistency.

### OrderLine - Entity within the Aggregate

> **Productive file:** [`SoftwareLearningGuide.Core.Business/Entities/OrderLine.cs`](SoftwareLearningGuide.Core.Business/Entities/OrderLine.cs)

> **Note:** Although `OrderLine` lives within the `Order` aggregate, its source file resides in `Entities/`, not in `Aggregates/`. The `Order` aggregate (in `Aggregates/`) references `OrderLine` as a contained entity.

```csharp
namespace SoftwareLearningGuide.Core.Business.Entities;

using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

/// <summary>
/// Entity that represents a line within an Order.
/// Part of the Order aggregate and should not be persisted independently.
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
    /// Creates an OrderLine instance with validation.
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
    /// Calculates the subtotal of this line (UnitPrice * Quantity).
    /// </summary>
    public Result<Money> GetSubtotal() => UnitPrice.Multiply(Quantity);

    /// <summary>
    /// Updates the line quantity. Returns Failure if the quantity is invalid.
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
```

---

## Producing Domain Events

Both `Product`, `Customer`, and `Order` inherit from `ProduceEvents`, which is the base class for Aggregate Roots and Entities that supports Domain Events. Each event accumulates in memory and is dispatched atomically before confirming the SQL transaction (via `SaveChangesInterceptor`).

> **Productive file:** [`SoftwareLearningGuide.Core.Business/DomainEvents/ProduceEvents.cs`](SoftwareLearningGuide.Core.Business/DomainEvents/ProduceEvents.cs)

```csharp
namespace SoftwareLearningGuide.Core.Business.DomainEvents;

/// <summary>
/// Base class for Aggregate Roots and Entities that supports Domain Events.
/// Each event accumulates in memory and is dispatched atomically
/// before confirming the SQL transaction (via SaveChangesInterceptor).
/// </summary>
public abstract class ProduceEvents {
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Read-only list of pending domain events to be dispatched.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the pending queue.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent) {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a specific event from the pending queue.
    /// </summary>
    public void RemoveDomainEvent(IDomainEvent domainEvent) {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all pending domain events.
    /// Invoked after all events have been dispatched.
    /// </summary>
    public void ClearDomainEvents() {
        _domainEvents.Clear();
    }
}
```

**Key points of ProduceEvents:**

- `AddDomainEvent(IDomainEvent)` — protected, entities call it to enqueue events
- `RemoveDomainEvent(IDomainEvent)` — public, allows removing a specific event from the queue
- `ClearDomainEvents()` — public, invoked after all events have been dispatched
- `DomainEvents` exposes a read-only list so the persistence interceptor can read pending events
- It's `abstract`, so only domain entities inherit from it

---

## Comparisons and Differences

The key is understanding when to use each one: **Value Objects** for concepts without identity (money, emails, addresses), **Entities** for objects with identity that change (products, customers), and **Aggregates** for groups of entities that must be consistent (order with its lines). They are not mutually exclusive: an Aggregate contains Entities, and Entities contain Value Objects. They are layers of abstraction that complement each other.

### Value Objects vs Entities

| Aspect | Value Object | Entity |
|--------|--------------|---------|
| **Identity** | No unique identity | Unique identity (ID) |
| **Equality** | By value | By ID |
| **Mutability** | Immutable | Mutable |
| **Lifecycle** | Has none | Born, lives, modified, dies |
| **Persistence** | Persisted as part of an entity | Persisted independently |
| **Example** | Money, Email, Address | Product, Customer, Order |
| **Creation** | `new Money(100, "USD")` | Factory method or constructor |
| **Validation** | In constructor | In constructor and methods |

### Entities vs Aggregates

| Aspect | Entity | Aggregate |
|--------|---------|----------|
| **Scope** | A single object | Group of related objects |
| **Root** | Not applicable | One entity is the root |
| **Access** | Direct access | Only through the root |
| **Consistency** | Individual responsibility | Shared responsibility |
| **Transactions** | Individual changes | Atomic changes |
| **Example** | Product, Customer | Order (with OrderLines) |

### Value Objects vs Primitives

❌ **With primitives:**
```csharp
public class Product
{
	public decimal Price { get; set; } // In what currency?
	public string Name { get; set; } // Validated?

	public decimal CalculateTotal(decimal quantity)
	{
		return Price * quantity; // What if Price is negative?
	}
}

// Problem: nothing guarantees Price is valid
var product = new Product { Price = -100, Name = "" };
```

✅ **With Value Objects:**
```csharp
public class Product
{
	public Money Price { get; set; } // Amount + Currency guaranteed
	public string Name { get; set; }

	public Result<Money> CalculateTotal(int quantity)
	{
		return Price.Multiply(quantity); // Returns Result
	}
}

// Constructor validates automatically
var product = new Product { Price = new Money(-100, "USD") };
// Throws: ArgumentException - Amount cannot be negative
```

---

## Project Examples

### 1. Complete Flow: Create an Order

```csharp
// Create Order aggregate
var orderId = OrderId.Create();
var customerId = CustomerId.Create();
var shippingAddress = new Address("123 Main St", "Madrid", "Madrid", "28001", "Spain");
var order = new Order(orderId, customerId, shippingAddress);

// Add products
var productId = ProductId.Create();
var product = new Product(
	productId,
	"Laptop",
	"High performance laptop",
	new Money(1000, "USD"),
	5);

var addResult = order.AddProduct(product, 2);
if (!addResult.IsSuccess)
{
	Console.WriteLine($"Error: {addResult.Error}");
	return;
}

// Calculate total
var totalResult = order.GetTotalAmount();
if (!totalResult.IsSuccess)
{
	Console.WriteLine($"Error calculating total: {totalResult.Error}");
	return;
}

Console.WriteLine($"Order total: {totalResult.Value}");

// Confirm order
var confirmResult = order.Confirm();
if (!confirmResult.IsSuccess)
{
	Console.WriteLine($"Cannot confirm: {confirmResult.Error}");
	return;
}

Console.WriteLine("Order confirmed successfully");
```

### 2. Invariant Protection

```csharp
// Attempt to add more than 10 products
var addResult = order.AddProduct(product, 15);
// Result: Failure - "Cannot add more than 10 units of the same product."

// Attempt to add negative quantity
var negativeResult = order.AddProduct(product, -1);
// Result: Failure - "Quantity must be greater than zero."

// Attempt to confirm an empty order
var emptyOrder = new Order(OrderId.Create(), customerId, shippingAddress);
var confirmResult = emptyOrder.Confirm();
// Result: Failure - "Cannot confirm an empty order."

// Attempt to confirm an already cancelled order
order.Cancel();
var confirmCancelledResult = order.Confirm();
// Result: Failure - "Cannot confirm an order in Cancelled status."

// Attempt to change address after confirming
var updateAddressResult = order.UpdateShippingAddress(newAddress);
// Result: Failure - "Cannot update shipping address in Confirmed status."
```

### 3. State Machine

```csharp
// The aggregate guarantees valid transitions
Console.WriteLine($"Initial status: {order.Status}"); // Pending

order.Confirm(); // Pending → Confirmed
Console.WriteLine($"After confirming: {order.Status}"); // Confirmed

order.Ship(); // Confirmed → Shipped
Console.WriteLine($"After shipping: {order.Status}"); // Shipped

order.Deliver(); // Shipped → Delivered
Console.WriteLine($"After delivering: {order.Status}"); // Delivered

// Attempt to confirm again after shipping
var confirmAgain = order.Confirm();
// Result: Failure - "Cannot confirm an order in Shipped status."

// Attempt to cancel after delivering
var cancelResult = order.Cancel();
// Result: Failure - "Cannot cancel an order in Delivered status."
```

---

## Benefits of DDD in This Project

✅ **Domain Security** - Business rules are guaranteed in code, not in the database  
✅ **Testability** - Easy to test without infrastructure dependencies  
✅ **Maintainability** - Code reflects the business language  
✅ **Reusability** - Value Objects and Entities can be used in multiple places  
✅ **Type-Safety** - Strongly-typed IDs prevent errors  
✅ **Performance** - Aggregates encapsulate cohesive data sets  

---

## Recommended Resources

- 📖 [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- 📖 [Implementing Domain-Driven Design - Vaughn Vernon](https://vaughnvernon.com/)
- 🎥 [DDD in .NET - Nick Chapsas](https://www.youtube.com/c/NickChapsas)
- 🔗 [Microsoft - Domain-Driven Design in C#](https://docs.microsoft.com/en-us/dotnet/architecture/domain-driven-design/)

---

**Last updated:** 2024  
**Project:** SoftwareLearningGuide.Core.Business