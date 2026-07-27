# Mediator Pattern

![Pattern](https://img.shields.io/badge/Pattern-Mediator-purple)
![GoF](https://img.shields.io/badge/Classification-Behavioral-lightgrey)
![MediatR](https://img.shields.io/badge/Library-MediatR-blueviolet)

The **Mediator Pattern** defines an object that encapsulates how a set of objects interact with each other. It promotes low coupling by preventing objects from referencing each other directly, allowing you to vary their interaction independently.

---

## Table of Contents

1. [What is the Mediator Pattern?](#what-is-the-mediator-pattern)
2. [MediatR as Mediator Implementation](#mediatr-as-mediator-implementation)
3. [Request/Response — IRequest and IRequestHandler](#requestresponse--irequest-and-irequesthandler)
4. [Notification — INotification and INotificationHandler](#notification--inotification-and-inotificationhandler)
5. [Pipeline Behaviors](#pipeline-behaviors)
6. [Registration and Auto-Discovery](#registration-and-auto-discovery)
7. [Pattern Diagram](#pattern-diagram)
8. [Difference between Send and Publish](#difference-between-send-and-publish)

---

## What is the Mediator Pattern?

> **"Define an object that encapsulates how a set of objects interact with each other."** — GoF

**Without Mediator:**

```
Controller → OrderCreateService
Controller → OrderValidationService
Controller → OrderRepository
Controller → EventBus
Controller → MetricsService
// The Controller knows and depends on 5 different services
// A change in any of them can break the Controller
```

**With Mediator:**

```
Controller → IMediator.Send(CreateOrderCommand)
               └─► MediatR automatically resolves → CreateOrderCommandHandler
// The Controller only knows IMediator and the Command
// The Handler can change without touching the Controller
```

---

## MediatR as Mediator Implementation

**MediatR** is the .NET library that implements the Mediator pattern:

- The **sender** (Controller) sends a `Request` or publishes a `Notification` to `IMediator`.
- MediatR automatically resolves the corresponding **handler** and invokes it.
- The sender **doesn't know** which handler exists or how it's implemented.

**Two modes of operation:**

| Mode | Method | Handlers Invoked | Usage |
|------|--------|-----------------|-------|
| **Request/Response** | `IMediator.Send()` | Exactly **one** | Commands and Queries |
| **Notification** | `IMediator.Publish()` | **All** handlers | Domain Events |

---

## Request/Response — IRequest and IRequestHandler

### Command (write)

```csharp
// The Command implements IRequest<TResponse>
// TResponse is the handler's return type
public sealed record CreateOrderCommand : IRequest<Result<Guid>>
{
    public Guid CustomerId { get; init; }
    public List<CreateOrderLineCommand> Lines { get; init; } = new();
}

// The Handler implements IRequestHandler<TRequest, TResponse>
public sealed class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderWriteRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Use case logic
        var order = Order.Create(OrderId.Create(), ...);
        await _repository.AddAsync(order.Value!, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success(order.Value!.Id.Value);
    }
}
```

### Query (read)

```csharp
// The Query also implements IRequest<TResponse>
public sealed record GetOrderQuery(Guid OrderId) : IRequest<Result<GetOrderQueryResponse>>;

// Its Handler uses Dapper instead of EF Core
public sealed class GetOrderQueryHandler
    : IRequestHandler<GetOrderQuery, Result<GetOrderQueryResponse>>
{
    private readonly IDbConnection _connection;

    public async Task<Result<GetOrderQueryResponse>> Handle(GetOrderQuery request, CancellationToken ct)
    {
        const string sql = "SELECT * FROM Orders WHERE Id = @OrderId";
        var order = await _connection.QueryFirstOrDefaultAsync<OrderDto>(sql, new { request.OrderId });
        return order is null
            ? Result<GetOrderQueryResponse>.Failure(DomainErrors.Order.NotFound(request.OrderId))
            : Result<GetOrderQueryResponse>.Success(new GetOrderQueryResponse { Order = order });
    }
}
```

### Controller — the sender

```csharp
// The Controller uses IMediator.Send() to dispatch
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken ct)
{
    // Dispatches the command — MediatR automatically resolves CreateOrderCommandHandler
    var result = await _mediator.Send(command, ct);

    return result.IsSuccess
        ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { orderId = result.Value })
        : BadRequest(new { error = result.Error });
}

[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    // Dispatches the query — MediatR automatically resolves GetOrderQueryHandler
    var result = await _mediator.Send(new GetOrderQuery(id), ct);

    return result.IsSuccess
        ? Ok(result.Value)
        : NotFound(new { error = result.Error });
}
```

---

## Notification — INotification and INotificationHandler

### Domain Event as Notification

**Domain Events** are published as `INotification`. MediatR invokes **all** registered handlers for that notification type.

```csharp
// The Domain Event implements INotification
public sealed class OrderCreatedDomainEvent : INotification
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### Multiple handlers for the same event

```csharp
// Handler 1: writes to Outbox to publish to RabbitMQ
public sealed class OrderCreatedNotificationHandler
    : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly IOutboxWriter _outboxWriter;

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken ct)
    {
        await _outboxWriter.WriteAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = notification.OrderId,
            CustomerId = notification.CustomerId
        }, ct);
    }
}

// Handler 2: records metrics (added without modifying Handler 1 or the domain)
public sealed class OrderCreatedMetricsHandler
    : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly OrderMetrics _metrics;

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken ct)
    {
        _metrics.OrderCreated(notification.OrderId, notification.CustomerId);
        await Task.CompletedTask;
    }
}

// Handler 3: audit logs (another handler, same event)
public sealed class OrderCreatedAuditHandler
    : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly ILogger<OrderCreatedAuditHandler> _logger;

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("Audit: Order {OrderId} created for Customer {CustomerId}",
            notification.OrderId, notification.CustomerId);
        await Task.CompletedTask;
    }
}
```

> **OCP Key:** To add new behavior when an order is created (like sending an email), you only add a new `INotificationHandler<OrderCreatedDomainEvent>`. **Without modifying** the domain, UnitOfWork, or existing handlers.

### How UnitOfWork dispatches Domain Events

```csharp
// UnitOfWork dispatches events before saving to DB
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // 1. Dispatch Domain Events (within the same transaction)
        await DispatchDomainEventsAsync(ct);

        // 2. Persist everything (Product + OutboxMessage in the same SQL transaction)
        return await _context.SaveChangesAsync(ct);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        // Finds all entities with pending events
        var entities = _context.ChangeTracker.Entries<ProduceEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        // IMediator.Publish() → invokes ALL registered INotificationHandlers
        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, ct);
    }
}
```

---

## Pipeline Behaviors

MediatR allows you to intercept the pipeline with `IPipelineBehavior<TRequest, TResponse>`. It's the equivalent of ASP.NET middleware, but for Commands and Queries.

```csharp
// Example: ValidationBehavior — automatically validates before executing the handler
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Before the handler: validate the request
        var context = new ValidationContext<TRequest>(request);
        var errors = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .ToList();

        if (errors.Any())
            throw new ValidationException(errors);

        // Continue to next behavior or actual handler
        return await next();
    }
}

// Registration in the container
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(...);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

---

## Registration and Auto-Discovery

MediatR discovers handlers automatically via assembly scanning:

```csharp
// SoftwareLearningGuide.Api/Startup/CqrsStartup.cs
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(CreateOrderCommandHandler).Assembly,   // Application.Command
    typeof(GetOrderQueryHandler).Assembly));       // Application.Query

// This automatically registers:
// - CreateOrderCommandHandler → IRequestHandler<CreateOrderCommand, Result<Guid>>
// - GetOrderQueryHandler → IRequestHandler<GetOrderQuery, Result<GetOrderQueryResponse>>
// - OrderCreatedNotificationHandler → INotificationHandler<OrderCreatedDomainEvent>
// - And all other handlers you add in the future
```

> **Open/Closed in action:** To add `CancelOrderCommand` and its handler, you only create the files. MediatR discovers and registers them automatically the next time it compiles. **Zero changes to the startup.**

---

## Pattern Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│  Senders (don't know the handlers)                                  │
│                                                                     │
│  OrderController         UnitOfWork                                 │
│  _mediator.Send(cmd)     _mediator.Publish(domainEvent)             │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                                 ▼
┌────────────────────────────────────────────────────────────────────┐
│  IMediator (Mediator)                                              │
│  Resolves and dispatches to the corresponding handler(s)           │
│                                                                    │
│  .Send()    → 1 handler (IRequestHandler)                          │
│  .Publish() → N handlers (INotificationHandler)                    │
└───────┬───────────────────────┬────────────────────────────────────┘
        │                       │
        ▼                       ▼
┌──────────────────┐  ┌─────────────────────────────────────────┐
│  IRequestHandler │  │  INotificationHandler (all invoke)      │
│                  │  │                                         │
│ CreateOrderCmd   │  │  OrderCreatedNotificationHandler        │
│   Handler        │  │  → writes to Outbox                     │
│                  │  │                                         │
│ GetOrderQuery    │  │  OrderCreatedMetricsHandler             │
│   Handler        │  │  → records metrics                      │
│                  │  │                                         │
│ CancelOrderCmd   │  │  OrderCreatedAuditHandler               │
│   Handler        │  │  → audit log                            │
└──────────────────┘  └─────────────────────────────────────────┘
```

---

## Difference between Send and Publish

| Aspect | `IMediator.Send()` | `IMediator.Publish()` |
|--------|-------------------|----------------------|
| **Handlers Invoked** | Exactly **1** | **All** registered |
| **Error if no handler** | Yes, throws exception | No, simply does nothing |
| **Return** | `Task<TResponse>` | `Task` (void) |
| **Usage** | Commands and Queries | Domain Events |
| **Parallelism** | Sequential | Sequential by default |

---

**See also:**
- [`README.CQRS.md`](README.CQRS.md) — CQRS with MediatR: Commands, Queries, and Handlers in detail
- [`README.Pattern.SOLID.md`](README.Pattern.SOLID.md) — OCP and DIP that Mediator implements
- [`README.EventSourcing.md`](../SoftwareLearningGuide.Core.Business/README.EventSourcing.md) — Domain Events published via Mediator
- [`README.UnitOfWork.md`](../SoftwareLearningGuide.Infraestructure/README.UnitOfWork.md) — How UoW uses Publish() to dispatch events