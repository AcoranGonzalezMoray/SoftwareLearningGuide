# Principios SOLID

![Pattern](https://img.shields.io/badge/Principios-SOLID-blueviolet)
![.NET](https://img.shields.io/badge/.NET-10-blue)

Los **principios SOLID** son cinco reglas de diseño orientado a objetos que, aplicadas juntas, producen código flexible, mantenible y testeable. Este documento muestra cómo cada principio se aplica de forma concreta en este proyecto.

---

## Tabla de Contenidos

1. [S — Single Responsibility Principle](#s--single-responsibility-principle)
2. [O — Open/Closed Principle](#o--openclosed-principle)
3. [L — Liskov Substitution Principle](#l--liskov-substitution-principle)
4. [I — Interface Segregation Principle](#i--interface-segregation-principle)
5. [D — Dependency Inversion Principle](#d--dependency-inversion-principle)
6. [SOLID en la práctica: crear una orden](#solid-en-la-práctica-crear-una-orden)
7. [Resumen Visual](#resumen-visual)

---

## S — Single Responsibility Principle

> **"Una clase debe tener una, y solo una, razón para cambiar."**

Cada clase tiene **una única responsabilidad**. Si necesitas cambiar cómo se persiste una orden, no deberías tocar la clase que valida las reglas de negocio.

### ✅ Aplicado en este proyecto

```
CreateOrderCommandHandler  → SOLO orquesta la creación de una orden
Order (agregado)           → SOLO contiene reglas de negocio de órdenes
OrderWriteRepository       → SOLO persiste órdenes con EF Core
UnitOfWork                 → SOLO gestiona la transacción y despacha eventos
OrderController            → SOLO traduce HTTP ↔ MediatR
GlobalExceptionMiddleware   → SOLO captura excepciones y responde con error estructurado
```

### ❌ Violación clásica

```csharp
// MAL: el controller hace demasiado
public class OrderController : ControllerBase
{
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        // Validación (responsabilidad 1)
        if (dto.Lines.Count == 0) return BadRequest("Sin líneas");

        // Lógica de negocio (responsabilidad 2)
        var order = new Order(dto.CustomerId);
        foreach (var line in dto.Lines)
            order.AddProduct(line.ProductId, line.Quantity);

        // Persistencia (responsabilidad 3)
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Publicación de evento (responsabilidad 4)
        await _bus.Publish(new OrderCreatedEvent { OrderId = order.Id });

        return Ok(order.Id);
    }
}
```

### ✅ Solución con SRP

```csharp
// BIEN: el controller tiene UNA sola responsabilidad: despachar
public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken ct)
{
    var result = await _mediator.Send(command, ct);  // ← Delega todo
    return result.IsSuccess
        ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { orderId = result.Value })
        : BadRequest(new { error = result.Error });
}
```

---

## O — Open/Closed Principle

> **"Las entidades deben estar abiertas para extensión, pero cerradas para modificación."**

Puedes añadir nueva funcionalidad **sin modificar código existente**.

### ✅ Aplicado en este proyecto

MediatR + Vertical Slicing aplican OCP de forma natural: añadir un nuevo Command/Query no requiere modificar ningún código existente.

```csharp
// Añadir "CancelOrder" no modifica NADA de CreateOrder, GetOrder, ni el Startup
// Solo añades archivos nuevos:

// CancelOrder/CancelOrderCommand.cs  (nuevo)
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result<bool>>;

// CancelOrder/CancelOrderCommandHandler.cs  (nuevo)
public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken ct) { ... }
}
```

MediatR registra automáticamente el nuevo handler por assembly scanning. **Zero modificación del código existente.**

### ✅ OCP en el dominio: Domain Events + Handlers

```csharp
// Añadir un nuevo comportamiento al crear un producto no requiere modificar Product ni UnitOfWork
// Solo añades un nuevo INotificationHandler<ProductCreatedDomainEvent>:

// (nuevo) EnviarEmailNotificationHandler.cs
public sealed class EnviarEmailNotificationHandler : INotificationHandler<ProductCreatedDomainEvent>
{
    public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken ct)
    {
        await _emailService.SendProductCreatedEmail(notification.Name);
    }
}

// MediatR descarga TODOS los handlers del mismo evento — sin modificar nada más
```

> **Clave:** El dominio (`Product`, `UnitOfWork`) no sabe cuántos handlers existen ni qué hacen. Es completamente extensible.

---

## L — Liskov Substitution Principle

> **"Los objetos de una clase derivada deben poder reemplazar objetos de su clase base sin alterar el comportamiento del programa."**

Cualquier implementación de un puerto (interfaz) debe ser intercambiable sin que el código que la usa se entere.

### ✅ Aplicado en este proyecto

```csharp
// El handler depende de IOrderWriteRepository (abstracción)
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderWriteRepository _repository;

    // En producción: inyecta OrderWriteRepository (EF Core + SQL Server)
    // En tests:      inyecta MockOrderWriteRepository (en memoria)
    // En futuro:     inyecta CosmosOrderWriteRepository (Azure CosmosDB)
    // El handler no necesita cambiar en ningún caso.
}
```

### ✅ LSP en los repositorios

```csharp
// IBaseRepository<TEntity, TId> es el contrato base
public interface IBaseRepository<TEntity, TId> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}

// IOrderWriteRepository extiende el contrato — y cualquier implementación
// que cumpla el contrato base puede reemplazar a otra sin romper nada
public interface IOrderWriteRepository : IBaseRepository<Order, Guid> { }

// BaseRepository<> implementa el contrato genérico
// OrderWriteRepository hereda y puede sustituir a cualquier IOrderWriteRepository
public sealed class OrderWriteRepository
    : BaseRepository<Order, OrderId, Guid>, IOrderWriteRepository { }
```

---

## I — Interface Segregation Principle

> **"Los clientes no deben verse obligados a depender de interfaces que no usan."**

Las interfaces deben ser pequeñas y enfocadas. Mejor muchas interfaces pequeñas que una grande con todo.

### ✅ Aplicado en este proyecto

```csharp
// IUnitOfWork tiene UNA sola responsabilidad: guardar cambios
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// IOrderWriteRepository tiene UNA sola responsabilidad: acceso a órdenes de escritura
public interface IOrderWriteRepository : IBaseRepository<Order, Guid> { }

// IOutboxWriter tiene UNA sola responsabilidad: escribir mensajes al outbox
public interface IOutboxWriter
{
    Task WriteAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
```

### ❌ Violación clásica

```csharp
// MAL: una interfaz que hace de todo — ISP violado
public interface IOrderService
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<List<Order>> GetAllAsync();
    Task CreateAsync(CreateOrderCommand command);
    Task CancelAsync(Guid id);
    Task<int> SaveChangesAsync();
    Task PublishEventAsync(IDomainEvent domainEvent);
}
// Problemas:
// 1. Un handler que solo lee órdenes igual debe implementar SaveChanges y PublishEvent
// 2. Difícil mockear en tests (hay que implementar métodos que no se usan)
// 3. Cambiar SaveChanges afecta a los tests de lectura
```

### ✅ Solución con ISP

```csharp
// BIEN: interfaces pequeñas y enfocadas
// El handler de lectura solo depende de lo que necesita
public sealed class GetOrderQueryHandler
{
    private readonly IDbConnection _connection; // Dapper directo — sin IOrderWriteRepository
}

// El handler de escritura depende de sus puertos específicos
public sealed class CreateOrderCommandHandler
{
    private readonly IOrderWriteRepository _repository;  // Solo escritura
    private readonly IUnitOfWork _unitOfWork;             // Solo Transaction
}
```

---

## D — Dependency Inversion Principle

> **"Los módulos de alto nivel no deben depender de módulos de bajo nivel. Ambos deben depender de abstracciones."**

El corazón de Clean Architecture. La capa de Aplicación define las interfaces; la Infraestructura las implementa.

### ✅ Aplicado en este proyecto

```
ALTO NIVEL (Application)   →  depende de  →  Abstracciones (Ports/Interfaces)
BAJO NIVEL (Infraestructure) →  implementa →  Abstracciones (Ports/Interfaces)
```

```csharp
// ALTO NIVEL: el handler no conoce EF Core ni SQL Server
public sealed class CreateOrderCommandHandler
{
    private readonly IOrderWriteRepository _repository;  // Abstracción
    private readonly IUnitOfWork _unitOfWork;             // Abstracción
    private readonly IOutboxWriter _outboxWriter;         // Abstracción

    // Si mañana cambias de EF Core a Dapper para escritura,
    // solo cambias la implementación concreta en Infraestructure.
    // Este handler NO cambia.
}

// BAJO NIVEL: la infraestructura implementa las abstracciones
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

### ✅ Registro de dependencias (Dependency Injection Container)

El contenedor de DI de ASP.NET Core es el "pegamento" que conecta abstracciones con implementaciones:

```csharp
// SoftwareLearningGuide.Infraestructure/Dependencies.cs
public static IServiceCollection AddInfraestructure(this IServiceCollection services, IConfiguration config)
{
    // "Cuando alguien pida IOrderWriteRepository, dale OrderWriteRepository"
    services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
    services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
    services.AddScoped<ICustomerWriteRepository, CustomerWriteRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<IOutboxWriter, OutboxWriter>();

    return services;
}
```

> **Beneficio directo:** En tests de integración o unitarios, cambias `OrderWriteRepository` por `FakeOrderWriteRepository` en el contenedor de tests. El handler **no sabe la diferencia**.

---

## SOLID en la práctica: crear una orden

Veamos cómo los 5 principios colaboran en una request `POST /api/v1/order`:

```
POST /api/v1/order
      │
      ▼
OrderController.Create()                    ← SRP: solo despacha HTTP
      │ _mediator.Send(CreateOrderCommand)
      ▼
CreateOrderCommandHandler.Handle()          ← SRP: solo orquesta el caso de uso
      │ new Order(...) — dominio puro        ← DIP: no conoce EF Core
      │ _repository.AddAsync(order)          ← DIP: depende de interfaz
      │ _unitOfWork.SaveChangesAsync()       ← DIP: depende de interfaz
      ▼
UnitOfWork.SaveChangesAsync()               ← SRP: solo gestiona transacción
      │ DispatchDomainEvents()
      │   IMediator.Publish(OrderCreated)   ← OCP: nuevos handlers sin modificar UoW
      │   NotificationHandler1.Handle()     ← ISP: handler pequeño y específico
      │   NotificationHandler2.Handle()     ← ISP: handler pequeño y específico
      ▼
ApplicationDbContext.SaveChangesAsync()     ← LSP: puede sustituirse por InMemoryDb en tests
```

---

## Resumen Visual

| Principio | Símbolo | Aplicado en | Beneficio |
|-----------|---------|-------------|-----------|
| **Single Responsibility** | S | Handler, Repository, Controller, Middleware | Cada clase cambia por una sola razón |
| **Open/Closed** | O | MediatR + Handlers, Domain Events | Añadir features sin modificar código existente |
| **Liskov Substitution** | L | IOrderWriteRepository, IUnitOfWork | Implementaciones intercambiables (test vs prod) |
| **Interface Segregation** | I | IUnitOfWork, IOutboxWriter, IOrderWriteRepository | Interfaces pequeñas y enfocadas |
| **Dependency Inversion** | D | Ports (interfaces en Application), Adapters (en Infraestructure) | Dominio desacoplado de framework y tecnología |

---

**Ver también:**
- [`README.Pattern.Factory.md`](README.Pattern.Factory.md) — Patrón Fábrica aplicado a Value Objects y Agregados
- [`README.Pattern.Builder.md`](README.Pattern.Builder.md) — Patrón Builder para construcción compleja de objetos
- [`README.CleanArchitecture.md`](../SoftwareLearningGuide.Api/README.CleanArchitecture.md) — Cómo SOLID estructura las capas
