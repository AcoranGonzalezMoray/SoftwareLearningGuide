# Factory Pattern

![Pattern](https://img.shields.io/badge/Pattern-Factory-yellow)
![GoF](https://img.shields.io/badge/Classification-Creational-lightgrey)

The **Factory Pattern** centralizes object creation in a specialized method, hiding construction logic from the client. Instead of using `new` directly, the client calls a `Create()` method that returns an already built and validated object.

---

## Table of Contents

1. [What is the Factory Pattern?](#what-is-the-factory-pattern)
2. [Types of Factory](#types-of-factory)
3. [Factory Method in Value Objects](#factory-method-in-value-objects)
4. [Factory Method in Aggregates](#factory-method-in-aggregates)
5. [Static Factory vs Constructor](#static-factory-vs-constructor)
6. [Factory in ID Creation](#factory-in-id-creation)
7. [Pattern Diagram](#pattern-diagram)
8. [Summary](#summary)

---

## What is the Factory Pattern?

> **"Define an interface for creating an object, but let subclasses decide which class to instantiate."** — GoF

In modern practice (especially in DDD), it's mainly used as a **Static Factory Method**: a static method on the class that returns a built and validated instance.

**Problem it solves:**

```csharp
// Without Factory: client builds directly and can create invalid objects
var money = new Money(-500, "")  // Negative price and empty currency — invalid object!
var email = new Email("this is not an email");  // No possible validation in public constructor
```

**With Factory:**

```csharp
// With Factory: creation passes through mandatory validation
var moneyResult = Money.Create(100, "USD");   // Returns Result<Money>
if (!moneyResult.IsSuccess) return moneyResult.Error;  // Explicit failure

var emailResult = Email.Create("user@domain.com");  // Validates format
var email = emailResult.Value!;  // You only access the value if it's valid
```

---

## Types of Factory

| Type | Description | In this project |
|------|-------------|-----------------|
| **Static Factory Method** | Static method on the class itself | `Money.Create()`, `Address.Create()`, `Order.Create()` |
| **Factory Method** (GoF) | Method in base class, subclasses override it | Doesn't apply directly |
| **Abstract Factory** | Family of related factories | Doesn't apply directly |
| **Factory Class** | Separate class just for creating objects | Doesn't apply directly |

This project primarily uses **Static Factory Method**, the cleanest pattern for Value Objects and Aggregates in DDD.

---

## Factory Method in Value Objects

**Value Objects** are immutable and can only be created in a valid state. The factory guarantees this invariant.

### Money

```csharp
// SoftwareLearningGuide.Core.Business/ValueObjects/Money.cs
public sealed class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    // PRIVATE constructor: no one can do new Money() directly
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // ← STATIC FACTORY METHOD: sole creation point
    public static Result<Money> Create(decimal amount, string currency)
    {
        // Centralized business validations
        if (amount < 0)
            return Result<Money>.Failure(DomainErrors.Money.NegativeAmount);

        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money>.Failure(DomainErrors.Money.InvalidCurrency);

        if (currency.Length != 3)
            return Result<Money>.Failure(DomainErrors.Money.CurrencyMustBeThreeChars);

        return Result<Money>.Success(new Money(amount, currency.ToUpperInvariant()));
    }
}
```

**Usage:**

```csharp
// In the handler — returns Result<Money>, doesn't throw exceptions
var priceResult = Money.Create(command.Price, command.Currency);
if (!priceResult.IsSuccess)
    return Result<Guid>.Failure(priceResult.Error!);

var price = priceResult.Value!;  // Only here you have the valid object
```

### Email

```csharp
public sealed class Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure(DomainErrors.Email.Required);

        // Validation with Regex or custom logic
        if (!email.Contains('@') || !email.Contains('.'))
            return Result<Email>.Failure(DomainErrors.Email.InvalidFormat);

        return Result<Email>.Success(new Email(email.ToLowerInvariant().Trim()));
    }
}
```

### Address

```csharp
public sealed class Address
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address(string street, string city, string state, string postalCode, string country)
    {
        Street = street; City = city; State = state;
        PostalCode = postalCode; Country = country;
    }

    // Factory accepts all fields and validates them together
    public static Result<Address> Create(
        string street, string city, string state, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Result<Address>.Failure(DomainErrors.Address.StreetRequired);

        if (string.IsNullOrWhiteSpace(city))
            return Result<Address>.Failure(DomainErrors.Address.CityRequired);

        if (string.IsNullOrWhiteSpace(country))
            return Result<Address>.Failure(DomainErrors.Address.CountryRequired);

        return Result<Address>.Success(new Address(street, city, state, postalCode, country));
    }
}
```

---

## Factory Method in Aggregates

**Aggregates** also use Factory Method to ensure they're created with all invariants covered and that they trigger the correct domain events.

### Order.Create()

```csharp
public sealed class Order : ProduceEvents
{
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; }
    public OrderStatus Status { get; private set; }

    // PRIVATE constructor: no one can create an "empty" Order
    private Order() { }

    // ← FACTORY METHOD on the Aggregate
    public static Result<Order> Create(OrderId id, CustomerId customerId, Address shippingAddress)
    {
        // Aggregate validations (business pre-conditions)
        if (id is null)
            return Result<Order>.Failure(DomainErrors.Order.InvalidId);

        if (customerId is null)
            return Result<Order>.Failure(DomainErrors.Order.CustomerRequired);

        if (shippingAddress is null)
            return Result<Order>.Failure(DomainErrors.Order.AddressRequired);

        var order = new Order
        {
            Id = id,
            CustomerId = customerId,
            ShippingAddress = shippingAddress,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Domain Event triggered at creation time
        order.AddDomainEvent(new OrderCreatedDomainEvent
        {
            OrderId = id.Value,
            CustomerId = customerId.Value,
            CreatedAt = order.CreatedAt
        });

        return Result<Order>.Success(order);
    }
}
```

**Usage in the handler:**

```csharp
// The handler uses the factory — never does new Order() directly
var orderResult = Order.Create(orderId, customerIdResult.Value!, addressResult.Value!);
if (!orderResult.IsSuccess)
    return Result<Guid>.Failure(orderResult.Error!);

var order = orderResult.Value!;
```

---

## Static Factory vs Constructor

| Aspect | Public Constructor | Static Factory Method |
|--------|-------------------|----------------------|
| **Validation** | Difficult to return Result — only throw | Can return `Result<T>` |
| **Descriptive Name** | No: always the class name | Yes: `Create()`, `FromEmail()`, `Of()` |
| **Immutability** | Can be violated from outside | Private constructor guarantees immutability |
| **Domain Events** | Difficult to trigger in constructor | Triggered in the factory |
| **Testability** | Coupled to exceptions | Result return facilitates testing |

```csharp
// Constructor: throws exception on failure
var money = new Money(-100, "USD");  // throws DomainException

// Factory: returns Result — no exceptions
var result = Money.Create(-100, "USD");
if (!result.IsSuccess)
    Console.WriteLine(result.Error?.Message);  // Explicit handling
```

> **Why prefer Factory with Result\<T\>?** Because exceptions break the normal control flow and are hard to test. A `Result<T>` is a value the compiler forces you to handle. Also, in a pipeline with multiple value objects (Address has 5 fields), you can return the first error without a huge `try/catch` block.

---

## Factory in ID Creation

**Identity Value Objects** also use factory:

```csharp
public sealed class OrderId
{
    public Guid Value { get; }

    // Internal constructor for reconstruction from DB
    internal OrderId(Guid value) => Value = value;

    // Factory to create a new ID (generates the GUID)
    public static OrderId Create() => new(Guid.NewGuid());

    // Factory to reconstruct from an existing Guid (from DB or request)
    public static Result<OrderId> From(Guid value)
    {
        if (value == Guid.Empty)
            return Result<OrderId>.Failure(DomainErrors.Order.InvalidId);

        return Result<OrderId>.Success(new OrderId(value));
    }
}
```

**Usage:**

```csharp
// Create a new order — generates ID automatically
var orderId = OrderId.Create();

// Reconstruct from an HTTP request GUID
var orderIdResult = OrderId.From(request.OrderId);
if (!orderIdResult.IsSuccess)
    return Result<Guid>.Failure(orderIdResult.Error!);
```

---

## Pattern Diagram

```
Client (Handler)
     │
     │  var result = Money.Create(100.0m, "USD")
     │
     ▼
┌──────────────────────────────────────────────────────┐
│  Money                          ← Class with Factory │
│                                                      │
│  private Money(decimal, string) ← Hidden constructor │
│                                                      │
│  + static Create(amount, currency)                   │
│    ├── Validate amount >= 0                          │
│    ├── Validate currency not empty                   │
│    ├── Validate currency == 3 chars                  │
│    └── return Result<Money>.Success(new Money(...))  │
└──────────────────────────────────────────────────────┘
     │
     ▼
Result<Money>
  IsSuccess = true
  Value = Money { Amount=100, Currency="USD" }
    — or —
  IsSuccess = false
  Error = DomainErrors.Money.NegativeAmount
```

---

## Summary

| Value Object | Factory | Validations |
|-------------|---------|-------------|
| `Money` | `Money.Create(amount, currency)` | amount >= 0, currency 3 chars, not empty |
| `Email` | `Email.Create(email)` | valid format, not empty |
| `Address` | `Address.Create(street, city, ...)` | required fields not empty |
| `OrderId` | `OrderId.Create()` / `OrderId.From(guid)` | guid != Guid.Empty |
| `Order` | `Order.Create(id, customerId, address)` | all required fields, triggers event |
| `Product` | `new Product(id, name, price, stock)` | validations in constructor + event |

---

**See also:**
- [`README.Pattern.Builder.md`](README.Pattern.Builder.md) — Builder pattern for step-by-step construction
- [`README.Pattern.SOLID.md`](README.Pattern.SOLID.md) — SOLID principles that justify using Factory
- [`README.ResultPattern.md`](../SoftwareLearningGuide.Core.Business/README.ResultPattern.md) — Result\<T\> returned by factories
- [`README.DDD.md`](../SoftwareLearningGuide.Core.Business/README.DDD.md) — Value Objects and Aggregates where factories live