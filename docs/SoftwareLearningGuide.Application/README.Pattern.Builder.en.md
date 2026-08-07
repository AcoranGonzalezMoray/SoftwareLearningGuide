# Builder Pattern

![Pattern](https://img.shields.io/badge/Pattern-Builder-orange)
![GoF](https://img.shields.io/badge/Classification-Creational-lightgrey)

The **Builder Pattern** constructs complex objects **step by step**. It separates the construction of an object from its final representation, allowing you to create variants of the same type with the same construction process.

---

#### Table of Contents

1. [What is the Builder Pattern?](#what-is-the-builder-pattern)
2. [Builder vs Factory](#builder-vs-factory)
3. [Fluent Builder for Tests](#fluent-builder-for-tests)
4. [Builder for Complex Commands](#builder-for-complex-commands)
5. [Implicit Builder: Record with](#implicit-builder-record-with)
6. [Pattern Diagram](#pattern-diagram)
7. [When to Use Builder vs Factory](#when-to-use-builder-vs-factory)

---

## What is the Builder Pattern?

> **"Separate the construction of a complex object from its representation, allowing the same construction process to create different representations."** — GoF

Builder is ideal when:
- The object has **many optional parameters**
- Construction requires **multiple steps** in a specific order
- You want to create variants of an object with different configurations
- You need **test objects** quickly preconfigured

---

## Builder vs Factory

| Aspect | Factory | Builder |
|--------|---------|---------|
| **Complexity** | Simple or medium objects | Complex objects with many fields |
| **Parameters** | Few, all mandatory | Many, several optional |
| **Step by step** | No — all at once | Yes — method by method |
| **Variants** | Few variants | Multiple configurations |
| **Typical Use** | Value Objects, Aggregates | Tests, complex configuration, DTOs |

---

## Fluent Builder for Tests

The most practical use of the Builder pattern in a project like this is **building test objects** without repeating initialization code in each test.

### OrderBuilder for Tests

```csharp
// Tests/Builders/OrderBuilder.cs
public sealed class OrderBuilder
{
    // Default values — any test has a valid Order as base
    private OrderId _id = OrderId.Create();
    private CustomerId _customerId = CustomerId.Create();
    private Address _address = Address.Create("123 Street", "Madrid", "Madrid", "28001", "ES").Value!;
    private OrderStatus _status = OrderStatus.Pending;
    private readonly List<(Product product, int quantity)> _lines = new();

    // Fluent methods — return this for chaining
    public OrderBuilder WithId(Guid id)
    {
        _id = OrderId.From(id).Value;
        return this;
    }

    public OrderBuilder WithCustomerId(Guid customerId)
    {
        _customerId = CustomerId.From(customerId).Value;
        return this;
    }

    public OrderBuilder WithShippingAddress(string street, string city, string country)
    {
        var result = Address.Create(street, city, "", "", country);
        _address = result.Value!;
        return this;
    }

    public OrderBuilder WithStatus(OrderStatus status)
    {
        _status = status;
        return this;
    }

    public OrderBuilder WithProduct(Product product, int quantity = 1)
    {
        _lines.Add((product, quantity));
        return this;
    }

    // Terminal method — builds the final object
    public Order Build()
    {
        var orderResult = Order.Create(_id, _customerId, _address);
        var order = orderResult.Value!;

        foreach (var (product, quantity) in _lines)
            order.AddProduct(product, quantity);

        if (_status == OrderStatus.Confirmed)
            order.Confirm();

        return order;
    }
}
```

**Usage in tests:**

```csharp
[Fact]
public async Task CreateOrder_WithValidData_ShouldSucceed()
{
    // Builder simplifies creation — no repeating parameters in each test
    var order = new OrderBuilder()
        .WithCustomerId(Guid.NewGuid())
        .WithShippingAddress("Gran Via 45", "Madrid", "ES")
        .WithProduct(new ProductBuilder().Build(), quantity: 3)
        .Build();

    Assert.Equal(OrderStatus.Pending, order.Status);
    Assert.Single(order.Lines);
}

[Fact]
public async Task ConfirmedOrder_ShouldHaveDomainEvent()
{
    var order = new OrderBuilder()
        .WithStatus(OrderStatus.Confirmed)
        .Build();

    Assert.Contains(order.DomainEvents, e => e is OrderCreatedDomainEvent);
}

[Fact]
public async Task OrderWithMultipleProducts_ShouldCalculateTotalCorrectly()
{
    var laptop = new ProductBuilder().WithPrice(1200, "USD").WithStock(5).Build();
    var mouse = new ProductBuilder().WithPrice(30, "USD").WithStock(10).Build();

    var order = new OrderBuilder()
        .WithProduct(laptop, quantity: 1)
        .WithProduct(mouse, quantity: 2)
        .Build();

    // Expected total: 1200 + 60 = 1260
    Assert.Equal(1260m, order.TotalAmount);
}
```

### ProductBuilder for Tests

```csharp
// Tests/Builders/ProductBuilder.cs
public sealed class ProductBuilder
{
    private ProductId _id = ProductId.Create();
    private string _name = "Test Product";
    private string _description = "Test description";
    private Money _price = Money.Create(100m, "USD").Value!;
    private int _stock = 10;

    public ProductBuilder WithName(string name) { _name = name; return this; }
    public ProductBuilder WithDescription(string desc) { _description = desc; return this; }
    public ProductBuilder WithPrice(decimal amount, string currency)
    {
        _price = Money.Create(amount, currency).Value!;
        return this;
    }
    public ProductBuilder WithStock(int stock) { _stock = stock; return this; }

    public Product Build() {
        var result = Product.Create(_id, _name, _description, _price, _stock);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"ProductBuilder failed: {result.Error}");
        return result.Value!;
    }
}
```

---

## Builder for Complex Commands

When a Command has many optional fields, a Builder makes the code more readable.

### CreateOrderCommandBuilder

```csharp
// Application.Command/CreateOrder/CreateOrderCommandBuilder.cs
public sealed class CreateOrderCommandBuilder
{
    private Guid _customerId = Guid.NewGuid();
    private string _street = "Main Street 1";
    private string _city = "Madrid";
    private string _state = "Madrid";
    private string _postalCode = "28001";
    private string _country = "ES";
    private readonly List<CreateOrderLineCommand> _lines = new();

    public CreateOrderCommandBuilder ForCustomer(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public CreateOrderCommandBuilder ShippingTo(
        string street, string city, string state, string postalCode, string country)
    {
        _street = street; _city = city; _state = state;
        _postalCode = postalCode; _country = country;
        return this;
    }

    public CreateOrderCommandBuilder AddLine(Guid productId, int quantity)
    {
        _lines.Add(new CreateOrderLineCommand { ProductId = productId, Quantity = quantity });
        return this;
    }

    public CreateOrderCommand Build() => new CreateOrderCommand
    {
        CustomerId = _customerId,
        ShippingStreet = _street,
        ShippingCity = _city,
        ShippingState = _state,
        ShippingPostalCode = _postalCode,
        ShippingCountry = _country,
        Lines = _lines
    };
}
```

**Usage:**

```csharp
var command = new CreateOrderCommandBuilder()
    .ForCustomer(customerId)
    .ShippingTo("Gran Via 45", "Madrid", "Madrid", "28013", "ES")
    .AddLine(productId1, quantity: 2)
    .AddLine(productId2, quantity: 1)
    .Build();

var result = await _mediator.Send(command);
```

---

## Implicit Builder: Record `with`

C# 9+ has a "lightweight Builder" built into records via the `with` expression, which creates a modified copy of an immutable record.

```csharp
// Command/Query records already act as immutable builders
public sealed record CreateProductCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public int Stock { get; init; }
}

// "with" acts as a single-line Builder — creates modified copies
var baseCommand = new CreateProductCommand { Name = "Laptop", Price = 1200, Currency = "USD", Stock = 5 };

// Variant 1: same product, different currency
var euroCommand = baseCommand with { Currency = "EUR", Price = 1100 };

// Variant 2: same product, no stock
var outOfStockCommand = baseCommand with { Stock = 0 };

// Usage in tests — very clean
[Theory]
[InlineData("USD", 1200)]
[InlineData("EUR", 1100)]
[InlineData("GBP", 950)]
public async Task CreateProduct_WithDifferentCurrencies_ShouldSucceed(string currency, decimal price)
{
    var command = baseCommand with { Currency = currency, Price = price };
    var result = await _handler.Handle(command, CancellationToken.None);
    Assert.True(result.IsSuccess);
}
```

---

## Pattern Diagram

```mermaid
graph TD
    A["Director (Test / Controller)"] -->|"new OrderBuilder().With...().Build()"| B

    B["OrderBuilder<br/>─────────────────────<br/>_id = OrderId.Create()  ← default values<br/>_customerId = CustomerId.Create()<br/>_address = ...valid address...<br/>_lines = []<br/>─────────────────────<br/>+ WithId(Guid) → return this<br/>+ WithCustomerId(Guid) → return this<br/>+ WithShippingAddress() → return this<br/>+ WithProduct(p, qty) → return this<br/>+ Build() → Order"]

    B -->|"Build()"| C

    C["Order — Object built step by step<br/>─────────────────────<br/>Id, CustomerId, ShippingAddress,<br/>Lines, Status"]
```

---

## When to Use Builder vs Factory

```mermaid
graph TD
    Q1{"Does the object have many<br/>optional fields?"}
    Q1 -->|"YES"| B1["Builder<br/>(more readable, more flexible)"]
    Q1 -->|"NO"| F1["Factory<br/>(more direct)"]

    Q2{"Do you need to create variants<br/>of the same object in tests?"}
    Q2 -->|"YES"| B2["Builder with default values"]
    Q2 -->|"NO"| F2["Static factory"]

    Q3{"Do the parameters have a<br/>specific construction order?"}
    Q3 -->|"YES"| B3["Builder with methods in order"]
    Q3 -->|"NO"| F3["Static factory"]

    Q4{"Is the object an immutable record<br/>with init properties?"}
    Q4 -->|"YES"| B4["record with {}<br/>(native C# lightweight Builder)"]
    Q4 -->|"NO"| F4["Classic Builder"]
```

---

**See also:**
- [`README.Pattern.Factory.md`](README.Pattern.Factory.md) — Factory pattern for Value Objects and Aggregates
- [`README.Pattern.SOLID.md`](README.Pattern.SOLID.md) — SOLID principles (SRP and OCP applied to Builder)
- [`README.ResultPattern.md`](../SoftwareLearningGuide.Core.Business/README.ResultPattern.md) — Result\<T\> returned by factories