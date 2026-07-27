# SOLID Principles

![Pattern](https://img.shields.io/badge/Principios-SOLID-blueviolet)
![.NET](https://img.shields.io/badge/.NET-10-blue)

The **SOLID principles** are five object-oriented design rules that, when applied together, produce flexible, maintainable, and testable code. This document shows how each principle is concretely applied in this project.

---

## Table of Contents

1. [S — Single Responsibility Principle](#s--single-responsibility-principle)
2. [O — Open/Closed Principle](#o--openclosed-principle)
3. [L — Liskov Substitution Principle](#l--liskov-substitution-principle)
4. [I — Interface Segregation Principle](#i--interface-segregation-principle)
5. [D — Dependency Inversion Principle](#d--dependency-inversion-principle)
6. [SOLID in Practice: Creating an Order](#solid-in-practice-creating-an-order)
7. [Visual Summary](#visual-summary)

---

## S — Single Responsibility Principle

> **"A class should have one, and only one, reason to change."**

Each class has a **single responsibility**. If you need to change how an order is persisted, you shouldn't have to touch the class that validates business rules.

### ✅ Applied in this project

```
CreateOrderCommandHandler  → ONLY orchestrates order creation
Order (aggregate)          → ONLY contains order business rules
OrderWriteRepository       → ONLY persists orders with EF Core
UnitOfWork                 → ONLY manages transactions and dispatches events
OrderController            → ONLY translates HTTP ↔ MediatR
GlobalExceptionMiddleware  → ONLY captures exceptions and responds with structured errors
```

### ❌ Classic violation

```csharp
// BAD: the controller does too much
public class OrderController : ControllerBase
{
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        // Validation (responsibility 1)
        if (dto.Lines.Count == 0) return BadRequest("No lines");

        // Business logic (responsibility 2)
        var order = new Order(dto.CustomerId);
        foreach (var line in dto.Lines)
            order.AddProduct(line.ProductId, line.Quantity);

        // Persistence (responsibility 3)
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Event publishing (responsibility 4)
        await _bus.Publish(new OrderCreatedEvent { OrderId = order.Id });

        return Ok(order.Id);
    }
}
```

### ✅ Solution with SRP

```csharp
// GOOD: the controller has ONE single responsibility: dispatch
public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken ct)
{
    var result = await _mediator.Send(command, ct);  // ← Delegates everything
    return result.IsSuccess
        ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { orderId = result.Value })
        : BadRequest(new { error = result.Error });
}
```

---

## O — Open/Closed Principle

> **"Entities should be open for extension, but closed for modification."**

You can add new functionality **without modifying existing code**.

### ✅ Applied in this project

MediatR + Vertical Slicing apply OCP naturally: adding a new Command/Query doesn't require modifying any existing code.

```csharp
// Adding "CancelOrder" doesn't modify ANYTHING in CreateOrder, GetOrder, or the Startup
// You only add new files:

// CancelOrder/CancelOrderCommand.cs  (new)
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result<bool>>;

// CancelOrder/CancelOrderCommandHandler.cs  (new)
public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken ct) { ... }
}
```

MediatR automatically registers the new handler via assembly scanning. **Zero modification of existing code.**

### ✅ OCP in the domain: Domain Events + Handlers

```csharp
// Adding new behavior when creating a product doesn't require modifying Product or UnitOfWork
// You only add a new INotificationHandler<ProductCreatedDomainEvent>:

// (new) SendEmailNotificationHandler.cs
public sealed class SendEmailNotificationHandler : INotificationHandler<ProductCreatedDomainEvent>
{
    public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken ct)
    {
        await _emailService.SendProductCreatedEmail(notification.Name);
    }
}

// MediatR dispatches ALL handlers for the same event — without modifying anything else
```

> **Key:** The domain (`Product`, `UnitOfWork`) doesn't know how many handlers exist or what they do. It's completely extensible.

---

## L — Liskov Substitution Principle

> **"Objects of a derived class should be able to replace objects of its base class without altering the correctness of the program."**

Any implementation of a port (interface) should be interchangeable without the code using it knowing.

### ✅ Applied in this project

```csharp
// The handler depends on IOrderWriteRepository (abstraction)
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderWriteRepository _repository;

    // In production: injects OrderWriteRepository (EF Core + SQL Server)
    // In tests:      injects MockOrderWriteRepository (in-memory)
    // In future:     injects CosmosOrderWriteRepository (Azure CosmosDB)
    // The handler doesn't need to change in any case.
}
```

### ✅ LSP in repositories

```csharp
// IBaseRepository<TEntity, TId> is the base contract
public interface IBaseRepository<TEntity, TId> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}

// IOrderWriteRepository extends the contract — and any implementation
// that fulfills the base contract can replace another without breaking anything
public interface IOrderWriteRepository : IBaseRepository<Order, Guid> { }

// BaseRepository<> implements the generic contract
// OrderWriteRepository inherits and can substitute any IOrderWriteRepository
public sealed class OrderWriteRepository
    : BaseRepository<Order, OrderId, Guid>, IOrderWriteRepository { }
```

---

## I — Interface Segregation Principle

> **"Clients should not be forced to depend on interfaces they don't use."**

Interfaces should be small and focused. Better many small interfaces than one large one with everything.

### ✅ Applied in this project

```csharp
// IUnitOfWork has ONE single responsibility: save changes
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// IOrderWriteRepository has ONE single responsibility: write access to orders
public interface IOrderWriteRepository : IBaseRepository<Order, Guid> { }

// IOutboxWriter has ONE single responsibility: write messages to the outbox
public interface IOutboxWriter
{
    Task WriteAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
```

### ❌ Classic violation

```csharp
// BAD: an interface that does everything — ISP violated
public interface IOrderService
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<List<Order>> GetAllAsync();
    Task CreateAsync(CreateOrderCommand command);
    Task CancelAsync(Guid id);
    Task<int> SaveChangesAsync();
    Task PublishEventAsync(IDomainEvent domainEvent);
}
// Problems:
// 1. A handler that only reads orders still must implement SaveChanges and PublishEvent
// 2. Difficult to mock in tests (must implement methods that aren't used)
// 3. Changing SaveChanges affects read tests
```

### ✅ Solution with ISP

```csharp
// GOOD: small, focused interfaces
// The read handler only depends on what it needs
public sealed class GetOrderQueryHandler
{
    private readonly IDbConnection _connection; // Direct Dapper — no IOrderWriteRepository
}

// The write handler depends on its specific ports
public sealed class CreateOrderCommandHandler
{
    private readonly IOrderWriteRepository _repository;  // Write only
    private readonly IUnitOfWork _unitOfWork;             // Transaction only
}
```

---

## D — Dependency Inversion Principle

> **"High-level modules should not depend on low-level modules. Both should depend on abstractions."**

The heart of Clean Architecture. The Application layer defines interfaces; Infrastructure implements them.

### ✅ Applied in this project

```
HIGH LEVEL (Application)    →  depends on  →  Abstractions (Ports/Interfaces)
LOW LEVEL (Infrastructure)  →  implements  →  Abstractions (Ports/Interfaces)
```

```csharp
// HIGH LEVEL: the handler doesn't know EF Core or SQL Server
public sealed class CreateOrderCommandHandler
{
    private readonly IOrderWriteRepository _repository;  // Abstraction
    private readonly IUnitOfWork _unitOfWork;             // Abstraction
    private readonly IOutboxWriter _outboxWriter;         // Abstraction

    // If tomorrow you switch from EF Core to Dapper for writes,
    // you only change the concrete implementation in Infrastructure.
    // This handler DOESN'T change.
}

// LOW LEVEL: infrastructure implements the abstractions
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint; // MassTransit

    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        await DispatchDomainEventsAsync();
        return await _context.SaveChangesAsync(ct);
    }
}
```

### ✅ Dependency registration (Dependency Injection Container)

ASP.NET Core's DI container is the "glue" that connects abstractions with implementations:

```csharp
// SoftwareLearningGuide.Infraestructure/Dependencies.cs
public static IServiceCollection AddInfraestructure(this IServiceCollection services, IConfiguration config)
{
    // "When someone asks for IOrderWriteRepository, give them OrderWriteRepository"
    services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
    services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
    services.AddScoped<ICustomerWriteRepository, CustomerWriteRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<IOutboxWriter, OutboxWriter>();

    return services;
}
```

> **Direct benefit:** In integration or unit tests, you swap `OrderWriteRepository` for `FakeOrderWriteRepository` in the test container. The handler **doesn't know the difference**.

---

## SOLID in Practice: Creating an Order

Let's see how the 5 principles collaborate in a `POST /api/v1/order` request:

```
POST /api/v1/order
      │
      ▼
OrderController.Create()                    ← SRP: only dispatches HTTP
      │ _mediator.Send(CreateOrderCommand)
      ▼
CreateOrderCommandHandler.Handle()          ← SRP: only orchestrates the use case
      │ new Order(...) — pure domain        ← DIP: doesn't know EF Core
      │ _repository.AddAsync(order)         ← DIP: depends on interface
      │ _unitOfWork.SaveChangesAsync()      ← DIP: depends on interface
      ▼
UnitOfWork.SaveChangesAsync()               ← SRP: only manages transaction
      │ DispatchDomainEvents()
      │   IMediator.Publish(OrderCreated)   ← OCP: new handlers without modifying UoW
      │   NotificationHandler1.Handle()     ← ISP: small, specific handler
      │   NotificationHandler2.Handle()     ← ISP: small, specific handler
      ▼
ApplicationDbContext.SaveChangesAsync()     ← LSP: can be replaced by InMemoryDb in tests
```

---

## Visual Summary

| Principle | Symbol | Applied in | Benefit |
|-----------|--------|------------|---------|
| **Single Responsibility** | S | Handler, Repository, Controller, Middleware | Each class changes for one reason only |
| **Open/Closed** | O | MediatR + Handlers, Domain Events | Add features without modifying existing code |
| **Liskov Substitution** | L | IOrderWriteRepository, IUnitOfWork | Interchangeable implementations (test vs prod) |
| **Interface Segregation** | I | IUnitOfWork, IOutboxWriter, IOrderWriteRepository | Small, focused interfaces |
| **Dependency Inversion** | D | Ports (interfaces in Application), Adapters (in Infrastructure) | Domain decoupled from framework and technology |

---

**See also:**
- [`README.Pattern.Factory.md`](README.Pattern.Factory.md) — Factory pattern applied to Value Objects and Aggregates
- [`README.Pattern.Builder.md`](README.Pattern.Builder.md) — Builder pattern for complex object construction
- [`README.CleanArchitecture.md`](../SoftwareLearningGuide.Api/README.CleanArchitecture.md) — How SOLID structures the layers