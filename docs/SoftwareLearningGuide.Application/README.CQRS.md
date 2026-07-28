# CQRS - Command Query Responsibility Segregation

![CQRS](https://img.shields.io/badge/Pattern-CQRS-green)
![MediatR](https://img.shields.io/badge/Library-MediatR-purple)

**CQRS** separa las operaciones de lectura (Query) de las de escritura (Command) en tu aplicación. En este proyecto usamos **MediatR** para despachar queries y commands a sus handlers correspondientes.

---

## Tabla de Contenidos

1. [¿Qué es CQRS?](#qué-es-cqrs)
2. [Por qué MediatR](#por-qué-mediatr)
3. [Flujo de una Request](#flujo-de-una-request)
4. [Queries - Lado de Lectura](#queries---lado-de-lectura)
5. [Commands - Lado de Escritura](#commands---lado-de-escritura)
6. [El Controller solo despacha](#el-controller-solo-despacha)
7. [Outbox Pattern](#outbox-pattern)
8. [Diagrama de Arquitectura](#diagrama-de-arquitectura)
9. [Referencia de Archivos](#referencia-de-archivos)

---

## ¿Qué es CQRS?

**CQRS** (Command Query Responsibility Segregation) es un patrón de diseño que separa el modelo de lectura del modelo de escritura:

- **Query**: Operación que lee datos sin modificar el estado. Devuelve un resultado.
- **Command**: Operación que modifica el estado. Puede o no devolver un resultado.

**Ejemplo práctico:** Imagina una tienda online. Cuando un cliente busca productos (query), necesita velocidad y puede ver datos cacheados. Pero cuando hace un pedido (command), necesita validación, transaccionalidad y tracking. CQRS permite optimizar cada caso por separado en lugar de usar el mismo modelo para ambos. Sin CQRS, tu endpoint de "buscar productos" y tu endpoint de "crear pedido" compartirían la misma lógica, forzando compromisos que perjudican ambos casos de uso.

### Beneficios

| Beneficio | Descripción |
|-----------|-------------|
| **Separación de responsabilidades** | Lecturas y escrituras tienen handlers independientes |
| **Optimización independiente** | Queries pueden usar Dapper (rápido), Commands usan EF Core (tracking) |
| **Escalabilidad** | Puedes escalar reads y writes de forma independiente |
| **Testabilidad** | Cada handler se puede testear de forma aislada |

---

## ¿Por qué MediatR?

**MediatR** implementa el patrón **Mediator** que se integra perfectamente con CQRS:

- El controller **no conoce** el handler que resuelve su request.
- El controller solo `IMediator.Send(request)` y MediatR resuelve automáticamente.
- Se registra por **assemblies** en [`SoftwareLearningGuide.Api/Startup/CqrsStartup.cs`](../SoftwareLearningGuide.Api/Startup/CqrsStartup.cs), no hay que registrar cada handler manualmente.

---

## Flujo de una Request

```
Controller                          MediatR                         Handler
     │                                  │                               │
     │  new GetOrderQuery(orderId)      │                               │
     │  ──────────────────────────────► │                               │
     │                                  │  IRequestHandler<...>.Handle  │
     │                                  │  ────────────────────────────►│
     │                                  │                               │
     │                                  │  Result<GetOrderQueryResponse>│
     │                                  │  ◄────────────────────────────│
     │  Result<GetOrderQueryResponse>   │                               │
     │  ◄──────────────────────────────│                                │
```

---

## Queries - Lado de Lectura

Un **Query** es un record que implementa `IRequest<TResponse>`:

> **¿Por qué un record?** Los records son inmutables por defecto, lo que garantiza que una vez creada la query no se pueda modificar durante su recorrido por el pipeline de MediatR. Esto es clave para la predictibilidad y thread-safety en escenarios concurrentes.

```csharp
// [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQuery.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQuery.cs)
namespace SoftwareLearningGuide.Application.Query.GetOrder;

public sealed record GetOrderQuery(Guid OrderId) : IRequest<Result<GetOrderQueryResponse>>;
```

### GetOrderQueryHandler

El handler resuelve la query usando **Dapper** para lectura directa, sin el overhead del change tracker de EF Core. En el lado de lectura queremos control total: saber exactamente qué SQL se ejecuta, hacer joins optimizados, y mapear directamente a DTOs.

> **¿Por qué el handler tiene SQL directo en lugar de usar EF Core?** Porque en el lado de lectura queremos control total: saber exactamente qué SQL se ejecuta, hacer joins optimizados, y mapear directamente a DTOs sin el overhead del change tracker de EF Core. EF Core está diseñado para rastrear cambios y persistir entidades; para lecturas puras, es innecesariamente pesado. Dapper ejecuta el SQL y devuelve objetos planos — nada más, nada menos. Esto es especialmente valioso cuando necesitas un JOIN complejo que EF Core representaría como múltiples consultas.

```csharp
// [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs)
public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Result<GetOrderQueryResponse>>
{
    private readonly IDbConnection _connection;

    public GetOrderQueryHandler(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Result<GetOrderQueryResponse>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        const string orderSql = @"
            SELECT
                o.Id, o.CustomerId, o.Status,
                o.ShippingStreet, o.ShippingCity, o.ShippingState,
                o.ShippingPostalCode, o.ShippingCountry,
                o.CreatedAt, o.ConfirmedAt, o.ShippedAt, o.DeliveredAt, o.CancelledAt,
                c.FirstName + ' ' + c.LastName AS CustomerName,
                c.Email AS CustomerEmail
            FROM Orders o
            INNER JOIN Customers c ON c.Id = o.CustomerId
            WHERE o.Id = @OrderId;";

        const string linesSql = @"
            SELECT
                ol.Id, ol.ProductId, ol.ProductName,
                ol.UnitPrice, ol.Currency, ol.Quantity,
                (ol.UnitPrice * ol.Quantity) AS Subtotal
            FROM OrderLines ol
            WHERE ol.OrderId = @OrderId;";

        var commandOrder = new CommandDefinition(
            orderSql,
            new { request.OrderId },
            cancellationToken: cancellationToken);

        var order = await _connection.QueryFirstOrDefaultAsync<OrderDto>(commandOrder);

        if (order is null)
            return Result<GetOrderQueryResponse>.Failure(DomainErrors.Order.NotFound(request.OrderId));

        var commandLines = new CommandDefinition(
            linesSql,
            new { request.OrderId },
            cancellationToken: cancellationToken);

        var lines = (await _connection.QueryAsync<OrderLineDto>(commandLines)).ToList();

        var result = order with {
            Lines = lines,
            TotalAmount = lines.Sum(l => l.Subtotal),
            TotalItems = lines.Sum(l => l.Quantity),
            LineCount = lines.Count,
            Currency = lines.FirstOrDefault()?.Currency ?? "USD"
        };

        return Result<GetOrderQueryResponse>.Success(new GetOrderQueryResponse { Order = result });
    }
}
```

**Nota sobre `GetOrderQueryResponse`:** Observa que el response vive en el namespace `SoftwareLearningGuide.Application.Query.GetOrderQuery` (con el sufijo "Query"), que es diferente del namespace `SoftwareLearningGuide.Application.Query.GetOrder` donde vive `GetOrderQuery`. Esto es una convención del proyecto: el response del query se agrupa en su propio namespace.

```csharp
// [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryResponse.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryResponse.cs)
namespace SoftwareLearningGuide.Application.Query.GetOrderQuery;

public sealed record GetOrderQueryResponse {
    public OrderDto Order { get; set; }
}
```

**¿Por qué dos SQLs en lugar de un JOIN con todos los datos?** Porque cargar las líneas de la orden en una consulta separada permite controlar el mapeo a DTOs diferentes (`OrderDto` vs `OrderLineDto`). También facilita el paginado futuro de líneas y permite agregar métricas calculadas (como `TotalAmount`, `TotalItems`) directamente en el handler sin afectar el mapeo del DTO principal.

---

## Commands - Lado de Escritura

Hemos visto el lado de lectura. Ahora veamos el lado de escritura, que es donde la magia de DDD ocurre. Los commands no solo guardan datos; validan reglas de negocio, crean agregados, y disparan domain events. Aquí es donde la arquitectura realmente brilla: cada command encapsula una intención del usuario que se traduce en cambios en el dominio.

Un **Command** es un record que implementa `IRequest<TResult>`:

```csharp
// [`SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommand.cs`](../../SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommand.cs)
namespace SoftwareLearningGuide.Application.Command.CreateOrder;

public sealed record CreateOrderCommand : IRequest<Result<Guid>>
{
    public Guid CustomerId { get; init; }
    public string ShippingStreet { get; init; } = string.Empty;
    public string ShippingCity { get; init; } = string.Empty;
    public string ShippingState { get; init; } = string.Empty;
    public string ShippingPostalCode { get; init; } = string.Empty;
    public string ShippingCountry { get; init; } = string.Empty;
    public List<CreateOrderLineCommand> Lines { get; init; } = new();
}

public sealed record CreateOrderLineCommand
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
```

### CreateOrderCommandHandler

El handler resuelve el command usando **EF Core** a través de repositorios, pero el guardado real se realiza a través de `IUnitOfWork` para garantizar transaccionalidad. El handler también depende de `ICustomerWriteRepository` y `IProductWriteRepository` para validar que el cliente y los productos existen antes de crear la orden.

> **¿Por qué el handler no guarda directamente en la base de datos?** Porque el handler no debería conocer la unidad de trabajo interna. El patrón UoW garantiza que todas las operaciones de repositorio (Insertar Order, actualizar Productos si aplica, etc.) se confirmen en una única transacción SQL. Si algo falla, todo se revierte — sin datoos parciales ni inconsistencias.

```csharp
// [`SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommandHandler.cs`](../../SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommandHandler.cs)
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderWriteRepository _orderRepository;
    private readonly ICustomerWriteRepository _customerRepository;
    private readonly IProductWriteRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderWriteRepository orderRepository,
        ICustomerWriteRepository customerRepository,
        IProductWriteRepository productRepository,
        IUnitOfWork unitOfWork) {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken) {
        var customerIdResult = CustomerId.From(request.CustomerId);
        if (!customerIdResult.IsSuccess)
            return Result<Guid>.Failure(customerIdResult.Error!);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Result<Guid>.Failure(DomainErrors.Customer.NotFound(request.CustomerId));

        var addressResult = Address.Create(
            request.ShippingStreet,
            request.ShippingCity,
            request.ShippingState,
            request.ShippingPostalCode,
            request.ShippingCountry);

        if (!addressResult.IsSuccess)
            return Result<Guid>.Failure(addressResult.Error!);

        var orderId = OrderId.Create();
        var orderResult = Order.Create(orderId, customerIdResult.Value!, addressResult.Value!);
        if (!orderResult.IsSuccess)
            return Result<Guid>.Failure(orderResult.Error!);

        var order = orderResult.Value!;

        foreach (var lineCommand in request.Lines) {
            var product = await _productRepository.GetByIdAsync(lineCommand.ProductId, cancellationToken);
            if (product is null)
                return Result<Guid>.Failure(DomainErrors.Product.NotFound(lineCommand.ProductId));

            var addResult = order.AddProduct(product, lineCommand.Quantity);
            if (!addResult.IsSuccess)
                return Result<Guid>.Failure(addResult.Error!);
        }

        var confirmResult = order.Confirm();
        if (!confirmResult.IsSuccess)
            return Result<Guid>.Failure(confirmResult.Error!);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(order.Id.Value);
    }
}
```

**Desglose paso a paso del handler:** Vamos a desglosar lo que hace este handler: Primero valida el `CustomerId` como value object y luego verifica que el cliente exista en la base de datos. Luego crea `Address` y `OrderId` como value objects, y construye el agregado `Order`. Después busca cada producto por su ID y lo agrega al pedido (validando stock y reglas). Luego confirma el pedido y persista todo a través de `IUnitOfWork.SaveChangesAsync()`. Cada paso puede fallar y devolver un `Result.Failure` descriptivo, sin excepciones no controladas. El evento de dominio `OrderCreatedDomainEvent` se dispara cuando la orden se confirma y es propagado por `UnitOfWork` a los `INotificationHandler` correspondientes.

### Puertos (Ports) - IBaseRepository, IOrderWriteRepository e IUnitOfWork

El handler depende de interfaces (puertos), no de implementaciones concretas. Veamos cada puerto:

> **¿Por qué separar `IBaseRepository` de los puertos específicos?** Porque `IBaseRepository<TEntity, TId>` define las operaciones CRUD genéricas que todas las entidades necesitan (`GetByIdAsync`, `AddAsync`). Cada write repository específico (como `IOrderWriteRepository`) hereda de este puerto base y puede agregar métodos específicos si son necesarios. Sin embargo, en este proyecto `IOrderWriteRepository` está vacía — hereda todo de `IBaseRepository<Order, Guid>`. Esto es intencional: si la entidad `Order` solo necesita operaciones CRUD genéricas, no tiene sentido duplicar las firmas en la interfaz. El principio DRY nos dice que la herencia ya comunica esa reutilización.

**`IBaseRepository<TEntity, TId>`** ([`SoftwareLearningGuide.Application/Ports/IBaseRepository.cs`](../../SoftwareLearningGuide.Application/Ports/IBaseRepository.cs)):

```csharp
namespace SoftwareLearningGuide.Application.Command.Ports;

public interface IBaseRepository<TEntity, TId> where TEntity : class {
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}
```

**`IOrderWriteRepository`** ([`SoftwareLearningGuide.Application/Ports/IOrderWriteRepository.cs`](../../SoftwareLearningGuide.Application/Ports/IOrderWriteRepository.cs)):

> **¿Por qué `IOrderWriteRepository` no tiene `SaveChangesAsync`?** Por qué el guardado se realiza a través de `IUnitOfWork`, no del repositorio. El patrón Unit of Work garantiza que múltiples operaciones de repositorio se confirmen en una única transacción. Si cada repositorio tuviera su propio `SaveChangesAsync`, no habría forma de agrupar operaciones atómicas entre repositorios diferentes (por ejemplo, actualizar un Order y un Product en la misma transacción). `IBaseRepository` y `IOrderWriteRepository` no tienen `SaveChangesAsync` — esa responsabilidad es de `IUnitOfWork`.

```csharp
namespace SoftwareLearningGuide.Application.Command.Ports;

public interface IOrderWriteRepository : IBaseRepository<Order, Guid> {
}
```

**`IUnitOfWork`** ([`SoftwareLearningGuide.Application/Ports/IUnitOfWork.cs`](../../SoftwareLearningGuide.Application/Ports/IUnitOfWork.cs)):

```csharp
namespace SoftwareLearningGuide.Application.Command.Ports;

public interface IUnitOfWork {
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**`IOutboxWriter`** ([`SoftwareLearningGuide.Application.Command/Ports/IOutboxWriter.cs`](../../SoftwareLearningGuide.Application.Command/Ports/IOutboxWriter.cs)):

```csharp
namespace SoftwareLearningGuide.Application.Command.Ports;

public interface IOutboxWriter {
    Task WriteAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
```

**¿Por qué el handler depende de interfaces y no de implementaciones?** Porque esto permite cambiar la implementación (de EF Core a Dapper, o a un mock para tests) sin modificar el handler. Es el Principio de Inversión de Dependencias en acción. El handler define QUÉ necesita (interfaz), y la infraestructura decide CÓMO se implementa. Cuando escribas tests para este handler, inyectarás mocks de `IOrderWriteRepository`, `ICustomerWriteRepository`, `IProductWriteRepository` e `IUnitOfWork` en lugar de una base de datos real.

### Implementaciones de Repositorios

Las implementaciones concretas viven en la capa de infraestructura. `BaseRepository<TEntity, TId, TIdValue>` proporciona las operaciones genéricas de EF Core, y `OrderWriteRepository` simplemente la hereda sin agregar nada nuevo:

```csharp
// [`SoftwareLearningGuide.Infraestructure/Repositories/BaseRepository.cs`](../../SoftwareLearningGuide.Infraestructure/Repositories/BaseRepository.cs)
public abstract class BaseRepository<TEntity, TId, TIdValue> : IBaseRepository<TEntity, TIdValue>
    where TEntity : class {
    // GetByIdAsync y AddAsync implementados con EF Core DbSet
}
```

```csharp
// [`SoftwareLearningGuide.Infraestructure/Repositories/OrderWriteRepository.cs`](../../SoftwareLearningGuide.Infraestructure/Repositories/OrderWriteRepository.cs)
public sealed class OrderWriteRepository : BaseRepository<Order, OrderId, Guid>, IOrderWriteRepository {
    public OrderWriteRepository(ApplicationDbContext context)
        : base(context, guid => new OrderId(guid)) {
    }
}
```

> **¿Por qué `BaseRepository` tiene `TIdValue` como parámetro genérico adicional?** Porque `OrderId` es un value object (no un `Guid` simple), y `GetByIdAsync` necesita convertir el `Guid` recibido en un `OrderId` internamente. El factory `_idFactory` maneja esa conversión de forma encapsulada.

---

## Outbox Pattern

El Outbox Pattern garantiza la entrega eventual de integration events de forma transaccional. Cuando un Domain Event se dispara dentro de una transacción de EF Core, `UnitOfWork.SaveChangesAsync()` primero despacha los eventos de dominio a los `INotificationHandler` correspondientes, y cada handler registra el Integration Event en la tabla `DomainOutboxMessages`. Luego, el `OutboxProcessor` (un_background_service separado) lee la tabla de outbox y publica los eventos en RabbitMQ.

> **¿Por qué el Outbox Pattern en lugar de publicar directamente?** Porque publicar un evento de integración directamente desde el handler rompe la transacción: si el handler falla después de publicar el evento pero antes de confirmar la transacción de base de datos, el evento se habría publicado pero el estado del dominio no se habría guardado — inconsistencia. El outbox asegura que el evento se escriba en la misma transacción que los datos del dominio, eliminando este riesgo.

**Flujo del Outbox:**

```
1. CreateOrderCommandHandler.Confirm() → dispara OrderCreatedDomainEvent
2. UnitOfWork.SaveChangesAsync() → dispara Domain Events dentro de la transacción
3. OrderCreatedNotificationHandler → escribe OrderCreatedEvent en DomainOutboxMessages
4. Context.SaveChangesAsync() → confirma todo en la misma transacción SQL
5. OutboxProcessor (BackgroundService) → lee DomainOutboxMessages y publica a RabbitMQ
```

---

## El Controller solo despacha

El controller **no conoce** los handlers. Solo crea la request y la envía a MediatR. En este proyecto, el controller está protegido por Feature Flags y tiene instrumentación de logging y métricas.

> **Archivo productivo:** [`SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs`](../SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs)

```csharp
// [`SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs`](../SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs)
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using SoftwareLearningGuide.Api.FeatureToggles;
using SoftwareLearningGuide.Api.Metrics;
using SoftwareLearningGuide.Application.Command.CreateOrder;
using SoftwareLearningGuide.Application.Query.GetAllOrders;
using SoftwareLearningGuide.Application.Query.GetOrder;
using SoftwareLearningGuide.Application.Query.GetOrderQuery;

namespace SoftwareLearningGuide.Api.Controllers.OrderControllerExample;

[ApiController]
[ApiVersion("1.0")]
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CONTROLLER)]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrderController : ControllerBase {
    private readonly IMediator _mediator;
    private readonly ILogger<OrderController> _logger;
    private readonly OrderMetrics _metrics;
    private readonly IFeatureManagerSnapshot _featureManager;

    public OrderController(IMediator mediator, ILogger<OrderController> logger, OrderMetrics metrics, IFeatureManagerSnapshot featureManager) {
        _mediator = mediator;
        _logger = logger;
        _metrics = metrics;
        _featureManager = featureManager;
    }

    [HttpGet]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_LIST)]
    [ProducesResponseType(typeof(GetAllOrdersQueryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", HttpContext.TraceIdentifier } })) {
            _logger.LogInformation("Iniciando obtencion de todas las ordenes");

            var query = new GetAllOrdersQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning("Error al obtener ordenes: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Se obtuvieron {Count} ordenes", result.Value!.TotalCount);
            return Ok(result.Value);
        }
    }

    [HttpGet("{id:guid}")]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_RETRIEVAL)]
    [ProducesResponseType(typeof(GetOrderQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object> { { "OrderId", id } })) {
            _logger.LogInformation("Iniciando obtención de orden {OrderId}", id);

            var query = new GetOrderQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning("Orden {OrderId} no encontrada: {Error}", id, result.Error);
                return NotFound(new { error = result.Error });
            }

            _logger.LogInformation("Orden {OrderId} obtenida exitosamente", id);
            return Ok(result.Value);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken) {

        if (await _featureManager.IsEnabledAsync(FeatureToggleNames.FT_ENABLE_ORDER_CREATION) is false) {
            _logger.LogWarning("Intento de crear orden mientras el módulo ORDERS_MODULE está deshabilitado.");
            return NotFound();
        }

        HttpContext.Items["CustomerId"] = command.CustomerId;
        HttpContext.Items["LineCount"] = command.Lines.Count;

        using (_logger.BeginScope(new Dictionary<string, object> { { "CustomerId", command.CustomerId }, { "LineCount", command.Lines.Count } })) {
            _logger.LogInformation(
                "Iniciando creación de orden para cliente {CustomerId} con {LineCount} líneas",
                command.CustomerId, command.Lines.Count);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning(
                    "Error al crear orden para cliente {CustomerId}: {Error}",
                    command.CustomerId, result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation(
                "Orden {OrderId} creada exitosamente para cliente {CustomerId}",
                result.Value, command.CustomerId);

            _metrics.OrderCreated(result.Value, command.CustomerId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value },
                new { orderId = result.Value });
        }
    }
}
```

**Nota sobre el controller:** Observa cómo el controller no tiene lógica de negocio. Solo crea la request, la envía a MediatR, y maneja la respuesta HTTP. Cada action tiene logging scopes con datos correlacionados (`CorrelationId`, `OrderId`, `CustomerId`, `LineCount`), lo que facilita el rastreo de requests en producción a través de los logs distribuidos. El uso de feature flags permite deshabilitar acciones completas o módulos completos sin desplegar nuevo código: si FT_ENABLE_ORDER_CREATION está deshabilitado, el endpoint POST retorna 404 en lugar de procesar el command.

---

## Registro de Dependencias (CQRS y Repositorios)

MediatR se registra por assemblies en [`SoftwareLearningGuide.Api/Startup/CqrsStartup.cs`](../SoftwareLearningGuide.Api/Startup/CqrsStartup.cs), escaneando los assemblies de los handlers:

```csharp
// [`SoftwareLearningGuide.Api/Startup/CqrsStartup.cs`](../SoftwareLearningGuide.Api/Startup/CqrsStartup.cs)
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(CreateOrderCommandHandler).Assembly,
    typeof(GetOrderQueryHandler).Assembly));
```

Los repositorios y la UoW se registran en [`SoftwareLearningGuide.Api/Startup/ReporitoriesStartup.cs`](../SoftwareLearningGuide.Api/Startup/ReporitoriesStartup.cs):

```csharp
// [`SoftwareLearningGuide.Api/Startup/ReporitoriesStartup.cs`](../SoftwareLearningGuide.Api/Startup/ReporitoriesStartup.cs)
services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
services.AddScoped<ICustomerWriteRepository, CustomerWriteRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

> **¿Por qué `RegisterServicesFromAssemblies` en lugar de registrar cada handler manualmente?** Porque MediatR soporta el descubrimiento automático de handlers por assembly. Al apuntar a los assemblies de `Application.Command` y `Application.Query`, cualquier handler nuevo se registra automáticamente sin tocar el código de bootstrapping. Esto es crucial para mantener el principio Open/Closed: agregar un nuevo command no requiere modificar el startup de la API.

---

## Diagrama de Arquitectura

```
┌─────────────────────────────────────────────────────────┐
│                    SoftwareLearningGuide.Api            │
│              OrderController (IMediator)                │
│                       │                                 │
│          ┌────────────┼────────────┐                    │
│          ▼            ▼            ▼                    │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐        │
│  │   GET All   │ │  GET by ID  │ │  POST create│        │
│  │   Query     │ │  Query      │ │  Command    │        │
│  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘        │
│         │               │               │               │
│         ▼               ▼               ▼               │
│  ┌─────────────────────────────────────────────────┐    │
│  │              MediatR (Mediator)                 │    │
│  │   Resuelve automáticamente el handler           │    │
│  └─────────────────────────────────────────────────┘    │
│         │               │               │               │
│         ▼               ▼               ▼               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐        │
│  │ Dapper      │ │ EF Core     │ │ EF Core     │        │
│  │ (Query)     │ │ (Command)   │ │ (Command)   │        │
│  └─────────────┘ └─────────────┘ └─────────────┘        │
│                                                    │    │
│         ┌──────────────────────────────────────────┘    │
│         ▼                                               │
│  ┌─────────────────────────────────────────────────┐    │
│  │            UnitOfWork + Outbox Pattern          │    │
│  │  SaveChangesAsync() → DispatchDomainEvents()    │    │
│  │  → INotificationHandler → IOutboxWriter         │    │
│  │  → DomainOutboxMessages table                   │    │
│  └─────────────────────────────────────────────────┘    │
│         │               │               │               │
│         ▼               ▼               ▼               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐        │ 
│  │ OrderRepo   │ │ ProductRepo │ │ CustomerRepo│        │
│  │ BaseRepo    │ │ BaseRepo    │ │ BaseRepo    │        │
│  └─────────────┘ └─────────────┘ └─────────────┘        │
└─────────────────────────────────────────────────────────┘
```

---

## Referencia de Archivos

| Archivo | Descripción |
|---------|-------------|
| [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQuery.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQuery.cs) | Query definition |
| [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs) | Query handler (Dapper) |
| [`SoftwareLearningGuide.Application.Query/GetOrder/OrderDto.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/OrderDto.cs) | DTOs de lectura |
| [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryResponse.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryResponse.cs) | Response wrapper |
| [`SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommand.cs`](../../SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommand.cs) | Command definition |
| [`SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommandHandler.cs`](../../SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommandHandler.cs) | Command handler (EF Core + UoW) |
| [`SoftwareLearningGuide.Application/Ports/IBaseRepository.cs`](../../SoftwareLearningGuide.Application/Ports/IBaseRepository.cs) | Puerto base genérico |
| [`SoftwareLearningGuide.Application/Ports/IOrderWriteRepository.cs`](../../SoftwareLearningGuide.Application/Ports/IOrderWriteRepository.cs) | Puerto de escritura para Order |
| [`SoftwareLearningGuide.Application/Ports/IUnitOfWork.cs`](../../SoftwareLearningGuide.Application/Ports/IUnitOfWork.cs) | Puerto de unidad de trabajo |
| [`SoftwareLearningGuide.Application/Ports/IOutboxWriter.cs`](../../SoftwareLearningGuide.Application.Command/Ports/IOutboxWriter.cs) | Puerto para escribir al outbox |
| [`SoftwareLearningGuide.Infraestructure/Repositories/BaseRepository.cs`](../../SoftwareLearningGuide.Infraestructure/Repositories/BaseRepository.cs) | Implementación base del repositorio |
| [`SoftwareLearningGuide.Infraestructure/Repositories/OrderWriteRepository.cs`](../../SoftwareLearningGuide.Infraestructure/Repositories/OrderWriteRepository.cs) | Implementación del puerto de Order |
| [`SoftwareLearningGuide.Infraestructure/Repositories/UnitOfWork.cs`](../../SoftwareLearningGuide.Infraestructure/Repositories/UnitOfWork.cs) | Implementación de la UoW + outbox dispatching |
| [`SoftwareLearningGuide.Infraestructure/Services/OutboxWriter.cs`](../../SoftwareLearningGuide.Infraestructure/Services/OutboxWriter.cs) | Implementación del outbox writer |
| [`SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs`](../SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs) | Controller que despacha |
| [`SoftwareLearningGuide.Api/Startup/CqrsStartup.cs`](../SoftwareLearningGuide.Api/Startup/CqrsStartup.cs) | Registro de MediatR |
| [`SoftwareLearningGuide.Api/Startup/ReporitoriesStartup.cs`](../SoftwareLearningGuide.Api/Startup/ReporitoriesStartup.cs) | Registro de repositorios y UoW |

---

## Paquetes NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| `MediatR` | 12.4.1 | Dispatcher de queries y commands |
| `Dapper` | 2.1.66 | Mapeo de SQL a objetos (lado query) |
| `Microsoft.EntityFrameworkCore` | 10.0.0 | ORM para lado command |
`Microsoft.Extensions.DependencyInjection` | 10.0.0 | Registro de dependencias |