# DomainErrors - Centralized Domain Error Catalog

This document explains the `DomainErrors` class implemented in `SoftwareLearningGuide.Core.Business.Errors` and why it's fundamental for maintainability, debugging, and a good user experience.

![Domain Errors](https://img.shields.io/badge/Pattern-Domain%20Errors-orange)

## Table of Contents

1. [Problem: Generic Errors](#problem-generic-errors)
2. [Solution: DomainErrors](#solution-domainerrors)
3. [Catalog Structure](#catalog-structure)
4. [Implementation](#implementation)
5. [Before vs After](#before-vs-after)
6. [Usage Examples](#usage-examples)
7. [Error Categories](#error-categories)
8. [Best Practices](#best-practices)

---

## Problem: Generic Errors

When validation errors are inline strings without context, debugging becomes a nightmare:

```csharp
// ❌ Generic messages - don't say WHICH entity failed or WHAT the ID is
if (quantity <= 0)
    return Result.Failure("Quantity must be greater than zero.");

if (price.Amount <= 0)
    return Result.Failure("Product price must be greater than zero.");

if (string.IsNullOrWhiteSpace(name))
    return Result.Failure("Name cannot be empty.");
```

**Concrete example:** Imagine you're on technical support and receive this error: *"Quantity must be greater than zero"*. Which product? In which order? For which customer? Without context, it's impossible to diagnose the problem quickly. You'd have to check logs, search the database, and ask the user what they did exactly. With a descriptive error, diagnosis reduces to an ID lookup.

### Consequences

| Problem | Impact |
|---------|--------|
| Don't know which entity failed | User can't identify the problem |
| No ID included | Impossible to reference the record in support |
| Duplicate strings | Same message appears in 5+ files |
| Hard to locate | If you change a message, you have to search everywhere |
| No centralized catalog | No single source of truth for errors |

---

## Solution: DomainErrors

`DomainErrors` is a static class with nested classes that centralizes **all** domain error messages. Each method generates a descriptive message with context of the affected entity and its ID.

**The idea is simple but powerful:** every error knows WHICH entity failed and WHAT the ID is. Instead of *"Quantity must be greater than zero"*, you get *"Quantity of Product with ID: a1b2c3d4-... must be greater than zero"*. Now support can look up the exact product in the database, check its history, and resolve the issue in minutes instead of hours.

### File

```
SoftwareLearningGuide.Core.Business/
  Errors/
    DomainErrors.cs       <-- Centralized catalog
```

---

## Catalog Structure

`DomainErrors` is organized by categories that reflect the domain's entities and value objects:

```
DomainErrors
├── Product           // Product entity errors
├── OrderLine         // Order Line entity errors
├── Order             // Order aggregate errors
├── Customer          // Customer entity errors
├── MoneyErrors       // Money Value Object errors
├── AddressErrors     // Address Value Object errors
├── EmailErrors       // Email Value Object errors
└── IdErrors          // ID Value Object errors
```

---

## Implementation

### General Definition

`DomainErrors` is a static class in the `SoftwareLearningGuide.Core.Business.Errors` namespace. Its purpose is to centralize all domain error messages in a single location, eliminating hardcoded strings distributed throughout the codebase. Each nested class represents an entity or value object, and each method generates a message with sufficient context for diagnosis.

> **Source file:** [`SoftwareLearningGuide.Core.Business/Errors/DomainErrors.cs`](SoftwareLearningGuide.Core.Business/Errors/DomainErrors.cs)

```csharp
namespace SoftwareLearningGuide.Core.Business.Errors;

/// <summary>
/// Centralized catalog of domain error messages.
/// Each method generates a descriptive message with context of the affected entity and its ID.
/// </summary>
public static class DomainErrors
{
    // Nested classes by category
    public static class Product { ... }
    public static class Order { ... }
    public static class Customer { ... }
    // etc.
}
```

### Example: Product Errors

Inside `DomainErrors.Product`, each message has two variants: one that receives the product's `Guid` (when the entity is known) and one without parameters (for generic contexts or constructors). This allows each error consumer to choose the level of detail it needs.

> **Source file:** [`SoftwareLearningGuide.Core.Business/Errors/DomainErrors.cs`](SoftwareLearningGuide.Core.Business/Errors/DomainErrors.cs)

```csharp
public static class Product
{
    // With ID - for when we have access to the Product
    public static string QuantityMustBeGreaterThanZero(Guid productId)
        => $"Quantity of Product with ID: {productId} must be greater than zero.";

    public static string PriceMustBeGreaterThanZero(Guid productId)
        => $"Price of Product with ID: {productId} must be greater than zero.";

    public static string InsufficientStock(Guid productId, int currentStock, int requested)
        => $"Insufficient stock for Product with ID: {productId}. " +
           $"Current stock: {currentStock}, requested: {requested}";

    // Without ID - for generic contexts or constructors
    public static string NameCannotBeEmpty()
        => "Product name cannot be empty.";
}
```

### Example: Order Errors

```csharp
public static class Order
{
    public static string QuantityMustBeGreaterThanZero(Guid productId)
        => $"Quantity of Product with ID: {productId} must be greater than zero.";

    public static string ExceedsMaxQuantityPerProduct(Guid productId, int maxQuantity)
        => $"Cannot add more than {maxQuantity} units of Product with ID: {productId}.";

    public static string RequiresPendingState(string currentState)
        => $"Operation requires Pending status. Current status: {currentState}";

    public static string CannotCancelOrderInState(string currentState)
        => $"Cannot cancel an Order in {currentState} status.";
}
```

### Example: Money Errors

```csharp
public static class MoneyErrors
{
    public static string AmountCannotBeNegative(decimal amount)
        => $"Money amount cannot be negative. Received amount: {amount}";

    public static string CannotAddDifferentCurrencies(string currency1, string currency2)
        => $"Cannot add amounts with different currencies: {currency1} and {currency2}";

    public static string CannotDivideByZero()
        => "Cannot divide a Money amount by zero.";
}
```

---

## Before vs After

### Example 1: Invalid quantity in OrderLine

**❌ Before:**
```csharp
// Entities/OrderLine.cs
if (quantity <= 0)
    throw new ArgumentException("Quantity must be greater than zero.");

// Result for the user:
// "Quantity must be greater than zero."
```

**✅ After:**
```csharp
// Entities/OrderLine.cs
using SoftwareLearningGuide.Core.Business.Errors;

if (quantity <= 0)
    throw new ArgumentException(
        DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(productId.Value));

// Result for the user:
// "Quantity of Product with ID: a1b2c3d4-... in the order line must be greater than zero."
```

**See the difference?** The message now includes the product ID. When a user reports an error, support can copy the ID and search directly in the database. This reduces debugging time from hours to minutes.

### Example 2: Insufficient stock

**❌ Before:**
```csharp
// Aggregates/Order.cs
if (!product.HasSufficientStock(quantity))
    return Result.Failure(
        $"Insufficient stock for '{product.Name}'. " +
        $"Available: {product.StockQuantity}, requested: {quantity}");
```

**✅ After:**
```csharp
// Aggregates/Order.cs
if (!product.HasSufficientStock(quantity))
    return Result.Failure(
        DomainErrors.Product.InsufficientStock(
            product.Name, product.StockQuantity, quantity));
```

**Benefit:** Before you had a hardcoded string in the aggregate code. Now the message comes from the centralized catalog. If tomorrow you decide to change the message format (e.g., add a link to documentation), you only modify `DomainErrors.cs` and all places using it update automatically.

### Example 3: Invalid Order status

**❌ Before:**
```csharp
// Aggregates/Order.cs
if (Status != OrderStatus.Confirmed)
    return Result.Failure(
        $"Only confirmed orders can be shipped. Current status: {Status}");
```

**✅ After:**
```csharp
// Aggregates/Order.cs
if (Status != OrderStatus.Confirmed)
    return Result.Failure(
        DomainErrors.Order.CannotShipOrderInState(Status.ToString()));
```

**Benefit:** The message now includes the current order status. If the error says *"Only confirmed orders can be shipped. Current status: Shipped"*, the user knows exactly what happened: the order was already shipped. Without the status, the user would have no idea why the operation failed.

### Example 4: Not found in Command Handler

**❌ Before:**
```csharp
// CreateOrderCommandHandler.cs
if (customer is null)
    return Result<Guid>.Failure($"Customer with ID {request.CustomerId} not found.");

if (product is null)
    return Result<Guid>.Failure($"Product with ID {lineCommand.ProductId} not found.");
```

**✅ After:**
```csharp
// CreateOrderCommandHandler.cs
if (customer is null)
    return Result<Guid>.Failure(DomainErrors.Customer.NotFound(request.CustomerId));

if (product is null)
    return Result<Guid>.Failure(DomainErrors.Product.NotFound(lineCommand.ProductId));
```

**Benefit:** "Not found" messages are now consistent across the entire application. It doesn't matter if the error comes from a controller, handler, or service: it always says *"[Entity] with ID: [id] not found"*. This consistency enables automatic error parsing and monitoring dashboard creation.

---

## Usage Examples

### In Constructors (throw ArgumentException)

```csharp
public OrderLine(OrderLineId id, ProductId productId, string productName, 
                  Money unitPrice, int quantity)
{
    // ...null checks...

    if (string.IsNullOrWhiteSpace(productName))
        throw new ArgumentException(
            DomainErrors.OrderLine.ProductNameCannotBeEmpty(productId.Value));

    if (unitPrice.Amount <= 0)
        throw new ArgumentException(
            DomainErrors.OrderLine.UnitPriceMustBeGreaterThanZero(productId.Value));

    if (quantity <= 0)
        throw new ArgumentException(
            DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(productId.Value));
}
```

### In Business Methods (return Result.Failure)

```csharp
public Result UpdateQuantity(int newQuantity)
{
    if (newQuantity <= 0)
        return Result.Failure(
            DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(ProductId.Value));

    Quantity = newQuantity;
    return Result.Success();
}
```

### In Command Handlers

```csharp
var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
if (customer is null)
    return Result<Guid>.Failure(DomainErrors.Customer.NotFound(request.CustomerId));

var product = await _productRepository.GetByIdAsync(lineCommand.ProductId, cancellationToken);
if (product is null)
    return Result<Guid>.Failure(DomainErrors.Product.NotFound(lineCommand.ProductId));
```

### In Status Validations

```csharp
private Result EnsurePending()
{
    if (Status != OrderStatus.Pending)
        return Result.Failure(
            DomainErrors.Order.RequiresPendingState(Status.ToString()));
    return Result.Success();
}
```

**Pattern to observe:** In constructors we use `DomainErrors` with `throw ArgumentException` (because the object cannot exist in an invalid state), but in business methods we use `Result.Failure` (because the error is recoverable and the flow can continue). This distinction is intentional: constructors protect critical invariants, while business methods handle errors the caller can decide how to resolve.

---

## Error Categories

> **Productive file:** [`SoftwareLearningGuide.Core.Business/Errors/DomainErrors.cs`](SoftwareLearningGuide.Core.Business/Errors/DomainErrors.cs)

### Product (16 methods)

| Method | Generated Message |
|--------|-------------------|
| `NameCannotBeEmpty(Guid)` | Product name with ID: {id} cannot be empty. |
| `NameCannotBeEmpty()` | Product name cannot be empty. |
| `DescriptionCannotBeEmpty(Guid)` | Product description with ID: {id} cannot be empty. |
| `DescriptionCannotBeEmpty()` | Product description cannot be empty. |
| `PriceMustBeGreaterThanZero(Guid)` | Price of Product with ID: {id} must be greater than zero. |
| `PriceMustBeGreaterThanZero()` | Product price must be greater than zero. |
| `PriceCannotBeNull()` | Product price cannot be null. |
| `StockCannotBeNegative(Guid)` | Stock quantity of Product with ID: {id} cannot be negative. |
| `QuantityMustBeGreaterThanZero(Guid)` | Quantity of Product with ID: {id} must be greater than zero. |
| `QuantityMustBeGreaterThanZero()` | Quantity must be greater than zero. |
| `QuantityToAddMustBeGreaterThanZero(Guid)` | Quantity to add of Product with ID: {id} must be greater than zero. |
| `QuantityToRemoveMustBeGreaterThanZero(Guid)` | Quantity to remove of Product with ID: {id} must be greater than zero. |
| `InsufficientStock(Guid, int, int)` | Insufficient stock for Product with ID: {id}. Current stock: {x}, requested: {y} |
| `InsufficientStock(string, int, int)` | Insufficient stock for '{name}'. Available: {x}, requested: {y} |
| `PriceValidationFailed(Guid)` | Error validating price of Product with ID: {id}. |
| `NotFound(Guid)` | Product with ID: {id} not found in the system. |

### OrderLine (6 methods)

| Method | Generated Message |
|--------|-------------------|
| `ProductNameCannotBeEmpty(Guid)` | Product name in line with ProductId: {id} cannot be empty. |
| `ProductNameCannotBeEmpty()` | Product name in line cannot be empty. |
| `UnitPriceMustBeGreaterThanZero(Guid)` | Unit price of Product with ID: {id} must be greater than zero. |
| `UnitPriceMustBeGreaterThanZero()` | Unit price must be greater than zero. |
| `QuantityMustBeGreaterThanZero(Guid)` | Quantity of Product with ID: {id} in order line must be greater than zero. |
| `QuantityMustBeGreaterThanZero()` | Order line quantity must be greater than zero. |

### Order (25 methods)

| Method | Generated Message |
|--------|-------------------|
| `ProductCannotBeNull()` | Product cannot be null when adding to Order. |
| `QuantityMustBeGreaterThanZero(Guid)` | Quantity of Product with ID: {id} must be greater than zero. |
| `QuantityCannotBeNegative(Guid)` | Quantity of Product with ID: {id} cannot be negative. |
| `ExceedsMaxQuantityPerProduct(Guid, int)` | Cannot add more than {n} units of Product with ID: {id}. |
| `ExceedsMaxQuantityPerProduct(int)` | Cannot exceed {n} units of the same product. |
| `ProductNotFoundInOrder(Guid)` | Product with ID: {id} does not exist in the Order. |
| `RequiresPendingState(string)` | Operation requires Pending status. Current status: {state} |
| `CannotConfirmEmptyOrder()` | Cannot confirm an empty Order. |
| `TotalAmountMustBeGreaterThanZero()` | Order total amount must be greater than zero. |
| `TotalAmountValidationFailed()` | Error validating Order total amount. |
| `CannotShipOrderInState(string)` | Only confirmed Orders can be shipped. Current status: {state} |
| `CannotDeliverOrderInState(string)` | Only shipped Orders can be delivered. Current status: {state} |
| `CannotCancelOrderInState(string)` | Cannot cancel an Order in {state} status. |
| `ShippingAddressCannotBeNull()` | Order shipping address cannot be null. |
| `RefundQuantityMustBeGreaterThanZero(Guid)` | Refund quantity of Product with ID: {id} must be greater than zero. |
| `RefundExceedsAvailable(Guid, int)` | Cannot refund units of Product with ID: {id}. Only {n} in the Order. |
| `NoLinesToCalculateAverage()` | No lines in the Order to calculate average. |
| `ErrorCalculatingSubtotal(int, string)` | Error calculating subtotal on line {i}: {error} |
| `PriceThresholdCannotBeNull()` | Price threshold cannot be null. |
| `MinimumOrderValueCannotBeNull()` | Minimum amount cannot be null. |
| `OtherTotalCannotBeNull()` | Other total cannot be null. |
| `NotFound(Guid)` | Order with ID: {id} not found. |
| `ProductIdCannotBeNull()` | ProductId cannot be null. |
| `ProductId1CannotBeNull()` | First ProductId cannot be null. |
| `ProductId2CannotBeNull()` | Second ProductId cannot be null. |

### Customer (8 methods)

| Method | Generated Message |
|--------|-------------------|
| `FirstNameCannotBeEmpty(Guid)` | Customer first name with ID: {id} cannot be empty. |
| `FirstNameCannotBeEmpty()` | Customer first name cannot be empty. |
| `LastNameCannotBeEmpty(Guid)` | Customer last name with ID: {id} cannot be empty. |
| `LastNameCannotBeEmpty()` | Customer last name cannot be empty. |
| `EmailCannotBeNull()` | Customer email cannot be null. |
| `ShippingAddressCannotBeNull()` | Customer shipping address cannot be null. |
| `NotFound(Guid)` | Customer with ID: {id} not found. |
| `NotFound(string)` | Customer with ID: {id} not found. |

### MoneyErrors (11 methods)

| Method | Generated Message |
|--------|-------------------|
| `AmountCannotBeNegative(decimal)` | Money amount cannot be negative. Received amount: {amount} |
| `CurrencyCannotBeEmpty()` | Currency cannot be empty. |
| `CurrencyMustBeThreeCharacters()` | Currency must be a 3-character code (e.g., USD, EUR, MXN). |
| `OtherMoneyCannotBeNull()` | Other Money value cannot be null. |
| `CannotAddDifferentCurrencies(string, string)` | Cannot add amounts with different currencies: {c1} and {c2} |
| `CannotSubtractDifferentCurrencies(string, string)` | Cannot subtract amounts with different currencies: {c1} and {c2} |
| `SubtractionResultCannotBeNegative(decimal)` | Subtraction result cannot be negative: {result} |
| `MultiplyFactorCannotBeNegative(decimal)` | Multiplication factor cannot be negative: {factor} |
| `CannotDivideByZero()` | Cannot divide a Money amount by zero. |
| `DivisorCannotBeNegative(decimal)` | Divisor cannot be negative: {divisor} |
| `CannotCompareDifferentCurrencies(string, string)` | Cannot compare amounts with different currencies: {c1} and {c2} |

### AddressErrors (6 methods)

| Method | Generated Message |
|--------|-------------------|
| `StreetCannotBeEmpty()` | Address street cannot be empty. |
| `CityCannotBeEmpty()` | Address city cannot be empty. |
| `StateCannotBeEmpty()` | Address state/province cannot be empty. |
| `PostalCodeCannotBeEmpty()` | Address postal code cannot be empty. |
| `CountryCannotBeEmpty()` | Address country cannot be empty. |
| `CannotBeNull()` | Address cannot be null. |

### EmailErrors (3 methods)

| Method | Generated Message |
|--------|-------------------|
| `CannotBeEmpty()` | Email address cannot be empty. |
| `CannotExceedMaxLength(int)` | Email address cannot exceed {maxLength} characters. |
| `InvalidFormat(string)` | Email address '{email}' has an invalid format. |

### IdErrors (4 methods)

| Method | Generated Message |
|--------|-------------------|
| `OrderIdCannotBeEmpty()` | OrderId cannot be an empty GUID. |
| `ProductIdCannotBeEmpty()` | ProductId cannot be an empty GUID. |
| `CustomerIdCannotBeEmpty()` | CustomerId cannot be an empty GUID. |
| `OrderLineIdCannotBeEmpty()` | OrderLineId cannot be an empty GUID. |

---

## Best Practices

### 1. Always use DomainErrors instead of inline strings

```csharp
// ❌ Avoid
return Result.Failure("Quantity must be greater than zero.");

// ✅ Prefer
return Result.Failure(DomainErrors.Product.QuantityMustBeGreaterThanZero(productId));
```

**Why?** Inline strings are duplicated throughout the codebase. If tomorrow you decide to change the message format, you'd have to search and replace in 20 different files. With DomainErrors, the change is in a single location. Additionally, the centralized catalog gives you a complete view of all possible system errors.

### 2. Prefer overload with ID when available

```csharp
// ✅ When we have access to the ID
DomainErrors.Product.NameCannotBeEmpty(product.Id.Value);

// ✅ When we DON'T have access to the ID (generic contexts)
DomainErrors.Product.NameCannotBeEmpty();
```

**Why prefer overload with ID?** Because when you're debugging an error at 3 AM, seeing *"Product with ID: a1b2c3d4-..."* tells you exactly where to look. Seeing just *"Product name cannot be empty"* tells you nothing. The ID is the bridge between the error and the solution.

### 3. Don't add new generic errors - use existing ones

Before creating a new method in `DomainErrors`, check if a similar one already exists. Consistency in messages is key.

**Why?** If you create `Product.NameIsEmpty()` and another developer creates `Product.NameCannotBeBlank()`, now you have two messages for the same error. Users will see inconsistent messages, and the support team won't know which is "official". Reusing existing errors keeps the business language coherent.

### 4. Keep messages consistent in tone and format

- Always mention the entity: "Product **with ID: {id}...**"
- Always include the ID when known
- Use infinitive verbs: "must be", "cannot be", "cannot"
- Format: `{Entity} {required action}. {additional context}`

**Why does format matter?** Because error messages are the interface between your system and the humans using it (users, support, developers). A consistent format enables creating monitoring tools that automatically parse errors, grouping similar errors in dashboards, and training the support team to respond quickly. If one day you say "cannot be empty" and the next "must be greater than zero", no one will know if they're the same error or different.

### 5. Constructors use DomainErrors with throw, business methods with Result.Failure

```csharp
// Constructor: protects invariants with exceptions
if (quantity <= 0)
    throw new ArgumentException(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(productId.Value));

// Business method: returns Result with the error
if (quantity <= 0)
    return Result.Failure(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(ProductId.Value));
```

**Why this distinction?** Because an object in an invalid state is a bug — it shouldn't exist. A recoverable error in a business method is normal flow — the user made a mistake and the system handles it gracefully. Mixing these two patterns (using `throw` in business methods or `Result` in constructors) creates confusion: either the system crashes when it shouldn't, or allows invalid objects when it shouldn't.

---

## Impact on Debugging

### Before (generic message)

```
Error creating order: Quantity must be greater than zero.
```

The developer doesn't know: Which product? In which line? In what context?

### After (descriptive message)

```
Error creating order: Quantity of Product with ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890 must be greater than zero.
```

The developer knows exactly which product failed and can trace the issue immediately.

---

**Last updated:** 2026
**Project:** SoftwareLearningGuide.Core.Business