# Patrón Mediator

![Pattern](https://img.shields.io/badge/Pattern-Mediator-purple)
![GoF](https://img.shields.io/badge/Clasificación-Comportamiento-lightgrey)
![MediatR](https://img.shields.io/badge/Library-MediatR-blueviolet)

El **Patrón Mediator** define un objeto que encapsula cómo un conjunto de objetos interactúan entre sí. Promueve el bajo acoplamiento evitando que los objetos se referencien directamente, permitiéndote variar su interacción de forma independiente.

---

#### Tabla de Contenidos

1. [¿Qué es el Patrón Mediator?](#qué-es-el-patrón-mediator)
2. [MediatR como implementación del Mediator](#mediatr-como-implementación-del-mediator)
3. [Request/Response — IRequest y IRequestHandler](#requestresponse--irequest-y-irequesthandler)
4. [Notification — INotification e INotificationHandler](#notification--inotification-e-inotificationhandler)
5. [Pipeline Behaviors](#pipeline-behaviors)
6. [Registro y Descubrimiento Automático](#registro-y-descubrimiento-automático)
7. [Diagrama del Patrón](#diagrama-del-patrón)
8. [Diferencia entre Send y Publish](#diferencia-entre-send-y-publish)

---

## ¿Qué es el Patrón Mediator?

> **"Define un objeto que encapsula cómo un conjunto de objetos interactúan entre sí."** — GoF

**Sin Mediator:**

```
Controller → OrderCreateService
Controller → OrderValidationService
Controller → OrderRepository
Controller → EventBus
Controller → MetricsService
// El Controller conoce y depende de 5 servicios distintos
// Un cambio en cualquiera de ellos puede romper el Controller
```

**Con Mediator:**

```
Controller → IMediator.Send(CreateOrderCommand)
               └─► MediatR resuelve automáticamente → CreateOrderCommandHandler
// El Controller solo conoce IMediator y el Command
// El Handler puede cambiar sin tocar el Controller
```

---

## MediatR como implementación del Mediator

**MediatR** es la librería .NET que implementa el patrón Mediator:

- El **emisor** (Controller) envía una `Request` o publica una `Notification` al `IMediator`.
- MediatR resuelve automáticamente el **handler** correspondiente y lo invoca.
- El emisor **no conoce** qué handler existe ni cómo se implementa.

**Dos modos de operación:**

| Modo | Método | Handlers invocados | Uso |
|------|--------|-------------------|-----|
| **Request/Response** | `IMediator.Send()` | Exactamente **uno** | Commands y Queries |
| **Notification** | `IMediator.Publish()` | **Todos** los handlers | Domain Events |

---

## Request/Response — IRequest y IRequestHandler

### Command (escritura)

```csharp
// El Command implementa IRequest<TResponse>
// TResponse es el tipo de retorno del handler
public sealed record CreateOrderCommand : IRequest<Result<Guid>>
{
    public Guid CustomerId { get; init; }
    public List<CreateOrderLineCommand> Lines { get; init; } = new();
}

// El Handler implementa IRequestHandler<TRequest, TResponse>
public sealed class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderWriteRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Lógica del caso de uso
        var order = Order.Create(OrderId.Create(), ...);
        await _repository.AddAsync(order.Value!, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success(order.Value!.Id.Value);
    }
}
```

### Query (lectura)

```csharp
// La Query también implementa IRequest<TResponse>
public sealed record GetOrderQuery(Guid OrderId) : IRequest<Result<GetOrderQueryResponse>>;

// Su Handler usa Dapper en lugar de EF Core
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

### Controller — el emisor

```csharp
// El Controller usa IMediator.Send() para despachar
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken ct)
{
    // Despacha el command — MediatR resuelve CreateOrderCommandHandler automáticamente
    var result = await _mediator.Send(command, ct);

    return result.IsSuccess
        ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { orderId = result.Value })
        : BadRequest(new { error = result.Error });
}

[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    // Despacha la query — MediatR resuelve GetOrderQueryHandler automáticamente
    var result = await _mediator.Send(new GetOrderQuery(id), ct);

    return result.IsSuccess
        ? Ok(result.Value)
        : NotFound(new { error = result.Error });
}
```

---

## Notification — INotification e INotificationHandler

### Domain Event como Notification

Los **Domain Events** se publican como `INotification`. MediatR invoca **todos** los handlers registrados para ese tipo de notificación.

```csharp
// El Domain Event implementa INotification
public sealed class OrderCreatedDomainEvent : INotification
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### Múltiples handlers del mismo evento

```csharp
// Handler 1: escribe al Outbox para publicar a RabbitMQ
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

// Handler 2: registra métricas (añadido sin modificar Handler 1 ni el dominio)
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

// Handler 3: logs de auditoría (otro handler, mismo evento)
public sealed class OrderCreatedAuditHandler
    : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly ILogger<OrderCreatedAuditHandler> _logger;

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("Auditoría: Orden {OrderId} creada para Cliente {CustomerId}",
            notification.OrderId, notification.CustomerId);
        await Task.CompletedTask;
    }
}
```

> **Clave OCP:** Para añadir un nuevo comportamiento cuando se crea una orden (como enviar un email), solo añades un nuevo `INotificationHandler<OrderCreatedDomainEvent>`. **Sin modificar** el dominio, ni UnitOfWork, ni los handlers existentes.

### Cómo UnitOfWork despacha los Domain Events

```csharp
// UnitOfWork despacha los eventos antes de guardar en BD
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // 1. Despachar Domain Events (dentro de la misma transacción)
        await DispatchDomainEventsAsync(ct);

        // 2. Persistir todo (Product + OutboxMessage en la misma transacción SQL)
        return await _context.SaveChangesAsync(ct);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        // Busca todas las entidades con eventos pendientes
        var entities = _context.ChangeTracker.Entries<ProduceEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        // IMediator.Publish() → invoca TODOS los INotificationHandler registrados
        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, ct);
    }
}
```

---

## Pipeline Behaviors

MediatR permite interceptar el pipeline con `IPipelineBehavior<TRequest, TResponse>`. Es el equivalente a los middlewares de ASP.NET, pero para Commands y Queries.

```csharp
// Ejemplo: ValidationBehavior — valida automáticamente antes de ejecutar el handler
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
        // Antes del handler: validar el request
        var context = new ValidationContext<TRequest>(request);
        var errors = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .ToList();

        if (errors.Any())
            throw new ValidationException(errors);

        // Continuar al siguiente behavior o al handler real
        return await next();
    }
}

// Registro en el contenedor
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(...);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

---

## Registro y Descubrimiento Automático

MediatR descubre handlers automáticamente por assembly scanning:

```csharp
// SoftwareLearningGuide.Api/Startup/CqrsStartup.cs
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(CreateOrderCommandHandler).Assembly,   // Application.Command
    typeof(GetOrderQueryHandler).Assembly));       // Application.Query

// Esto registra automáticamente:
// - CreateOrderCommandHandler → IRequestHandler<CreateOrderCommand, Result<Guid>>
// - GetOrderQueryHandler → IRequestHandler<GetOrderQuery, Result<GetOrderQueryResponse>>
// - OrderCreatedNotificationHandler → INotificationHandler<OrderCreatedDomainEvent>
// - Y todos los demás handlers que añadas en el futuro
```

> **Open/Closed en acción:** Para añadir `CancelOrderCommand` y su handler, solo creas los archivos. MediatR los descubre y registra automáticamente la próxima vez que se compila. **Zero cambios en el startup.**

---

## Diagrama del Patrón

```
┌─────────────────────────────────────────────────────────────────────┐
│  Emisores (desconocen los handlers)                                 │
│                                                                     │
│  OrderController         UnitOfWork                                 │
│  _mediator.Send(cmd)     _mediator.Publish(domainEvent)             │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                                 ▼
┌────────────────────────────────────────────────────────────────────┐
│  IMediator (Mediator)                                              │
│  Resuelve y despacha al/los handler(s) correspondiente(s)          │
│                                                                    │
│  .Send()    → 1 handler (IRequestHandler)                          │
│  .Publish() → N handlers (INotificationHandler)                    │
└───────┬───────────────────────┬────────────────────────────────────┘
        │                       │
        ▼                       ▼
┌──────────────────┐  ┌─────────────────────────────────────────┐
│  IRequestHandler │  │  INotificationHandler (todos invocan)   │
│                  │  │                                         │
│ CreateOrderCmd   │  │  OrderCreatedNotificationHandler        │
│   Handler        │  │  → escribe al Outbox                    │
│                  │  │                                         │
│ GetOrderQuery    │  │  OrderCreatedMetricsHandler             │
│   Handler        │  │  → registra métricas                    │
│                  │  │                                         │
│ CancelOrderCmd   │  │  OrderCreatedAuditHandler               │
│   Handler        │  │  → log de auditoría                     │
└──────────────────┘  └─────────────────────────────────────────┘
```

---

## Diferencia entre Send y Publish

| Aspecto | `IMediator.Send()` | `IMediator.Publish()` |
|---------|-------------------|----------------------|
| **Handlers invocados** | Exactamente **1** | **Todos** los registrados |
| **Error si no hay handler** | Sí, lanza excepción | No, simplemente no hace nada |
| **Retorno** | `Task<TResponse>` | `Task` (void) |
| **Uso** | Commands y Queries | Domain Events |
| **Paralelismo** | Secuencial | Secuencial por defecto |

---

**Ver también:**
- [`README.CQRS.md`](README.CQRS.md) — CQRS con MediatR: Commands, Queries y Handlers en detalle
- [`README.Pattern.SOLID.md`](README.Pattern.SOLID.md) — OCP y DIP que el Mediator implementa
- [`README.EventSourcing.md`](../SoftwareLearningGuide.Core.Business/README.EventSourcing.md) — Domain Events publicados via Mediator
- [`README.UnitOfWork.md`](../SoftwareLearningGuide.Infraestructure/README.UnitOfWork.md) — Cómo UoW usa Publish() para despachar eventos
