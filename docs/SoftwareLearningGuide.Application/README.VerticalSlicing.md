# Vertical Slicing Architecture

![Pattern](https://img.shields.io/badge/Architecture-Vertical%20Slicing-blue)
![MediatR](https://img.shields.io/badge/Library-MediatR-purple)
![CQRS](https://img.shields.io/badge/Pattern-CQRS-green)

**Vertical Slicing** organiza el código por **funcionalidades completas** (features) en lugar de hacerlo por capas técnicas horizontales. Cada "slice" contiene todo lo necesario para implementar una funcionalidad: query/command, handler, DTO, validación y respuesta, todos juntos en la misma carpeta.

---

## Tabla de Contenidos

1. [¿Qué es Vertical Slicing?](#qué-es-vertical-slicing)
2. [Horizontal vs Vertical](#horizontal-vs-vertical)
3. [Estructura en este Proyecto](#estructura-en-este-proyecto)
4. [Anatomía de un Slice](#anatomía-de-un-slice)
5. [Slice de Query: GetOrder](#slice-de-query-getorder)
6. [Slice de Command: CreateOrder](#slice-de-command-createorder)
7. [Cómo añadir un nuevo Slice](#cómo-añadir-un-nuevo-slice)
8. [Diagrama de Arquitectura](#diagrama-de-arquitectura)
9. [Beneficios y Trade-offs](#beneficios-y-trade-offs)
10. [Referencia de Archivos](#referencia-de-archivos)

---

## ¿Qué es Vertical Slicing?

**Vertical Slicing** es un estilo de organización de código donde cada funcionalidad del negocio agrupa todos sus artefactos en una misma carpeta o módulo.

> **Analogía:** Imagina un pastel de capas (layered). La arquitectura tradicional lo corta horizontalmente: una capa de bizcocho, una de crema, otra de bizcocho. Vertical Slicing lo corta en porciones verticales: cada porción incluye su propia pieza de bizcocho, crema y decoración. Cada porción es completamente autónoma e independiente.

En lugar de esto:

```
Application/
├── Commands/
│   ├── CreateOrderCommand.cs
│   ├── CreateProductCommand.cs
│   └── CreateCustomerCommand.cs
├── Queries/
│   ├── GetOrderQuery.cs
│   ├── GetProductQuery.cs
│   └── GetCustomerQuery.cs
└── Handlers/
    ├── CreateOrderCommandHandler.cs
    └── GetOrderQueryHandler.cs
```

Tenemos esto:

```
Application/
├── GetOrder/
│   ├── GetOrderQuery.cs
│   ├── GetOrderQueryHandler.cs
│   └── GetOrderQueryResponse.cs
└── CreateOrder/
    ├── CreateOrderCommand.cs
    ├── CreateOrderCommandHandler.cs
    └── CreateOrderCommandResponse.cs
```

---

## Horizontal vs Vertical

| Criterio | Horizontal (Layers) | Vertical (Slices) |
|----------|---------------------|-------------------|
| **Organización** | Por tipo técnico (Commands/, Queries/, Handlers/) | Por funcionalidad de negocio (GetOrder/, CreateOrder/) |
| **Cohesión** | Baja: archivos relacionados están en carpetas distintas | Alta: todo lo de una feature está junto |
| **Acoplamiento** | Alto entre capas técnicas | Bajo entre features |
| **Navegación** | Necesitas saltar entre múltiples carpetas | Todo está en un lugar |
| **Escalabilidad** | Las carpetas crecen indefinidamente | Cada feature es independiente |
| **Equipos** | Difícil trabajar en paralelo sin conflictos | Equipos pueden trabajar en features distintas sin conflicto |

> **¿Por qué Vertical Slicing en este proyecto?** Porque cuando necesitas entender o modificar "cómo se crea una orden", abres `CreateOrder/` y tienes todo ahí: el command, el handler, los DTOs. No necesitas buscar en tres carpetas distintas, recordar convenciones de nombres, ni coordinar con otros archivos dispersos. La cohesión funcional supera a la cohesión técnica en aplicaciones medianas y grandes.

---

## Estructura en este Proyecto

Este proyecto implementa Vertical Slicing a nivel de los proyectos `Application.Command` y `Application.Query`:

```
SoftwareLearningGuide.Application.Command/    ← Slices de escritura
├── CreateOrder/
│   ├── CreateOrderCommand.cs                 ← Definición del command
│   ├── CreateOrderCommandHandler.cs          ← Lógica de la feature
│   └── CreateOrderCommandResponse.cs        ← DTO de respuesta
├── CreateProduct/
│   ├── CreateProductCommand.cs
│   ├── CreateProductCommandHandler.cs
│   └── CreateProductCommandResponse.cs
├── CreateCustomer/
│   ├── CreateCustomerCommand.cs
│   ├── CreateCustomerCommandHandler.cs
│   └── CreateCustomerCommandResponse.cs
└── Ports/                                    ← Interfaces compartidas (no es un slice)
    ├── IBaseRepository.cs
    ├── IOrderWriteRepository.cs
    └── IUnitOfWork.cs

SoftwareLearningGuide.Application.Query/      ← Slices de lectura
├── GetOrder/
│   ├── GetOrderQuery.cs                      ← Definición de la query
│   ├── GetOrderQueryHandler.cs               ← Lógica de lectura (Dapper)
│   ├── GetOrderQueryResponse.cs              ← DTO de respuesta
│   └── OrderDto.cs                           ← DTO de datos
├── GetAllOrders/
│   ├── GetAllOrdersQuery.cs
│   ├── GetAllOrdersQueryHandler.cs
│   └── GetAllOrdersQueryResponse.cs
└── GetProduct/
    ├── GetProductQuery.cs
    ├── GetProductQueryHandler.cs
    └── GetProductQueryResponse.cs
```

> **Nota sobre `Ports/`:** La carpeta `Ports/` es una excepción a la regla. Las interfaces como `IBaseRepository<T>` y `IUnitOfWork` son contratos compartidos entre múltiples features y no pertenecen a un slice específico. Sin embargo, cada slice **referencia** estos puertos, no los duplica.

---

## Anatomía de un Slice

Cada slice tiene una responsabilidad única y un conjunto de archivos predecible:

```
CreateOrder/
├── CreateOrderCommand.cs          ← Datos de entrada (record inmutable)
│                                     Implementa IRequest<Result<Guid>>
├── CreateOrderCommandHandler.cs   ← Toda la lógica de negocio de la feature
│                                     Implementa IRequestHandler<TCommand, TResult>
└── CreateOrderCommandResponse.cs  ← Datos de salida (DTO)
                                      Solo contiene propiedades, sin lógica
```

**Reglas de un slice bien diseñado:**

1. **Un slice, una responsabilidad**: `CreateOrder` solo sabe crear órdenes.
2. **Sin dependencias entre slices**: `CreateOrder` no importa nada de `GetOrder`.
3. **Aislamiento de tecnología**: el handler puede cambiar de EF Core a Dapper sin afectar otros slices.
4. **Contratos vía Ports**: si necesitas algo de infraestructura, úsalo a través de una interfaz en `Ports/`.

---

## Slice de Query: GetOrder

Un slice de **lectura** es el más simple. Contiene una query, su handler y el DTO de respuesta.

### GetOrderQuery.cs

```csharp
// SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQuery.cs
namespace SoftwareLearningGuide.Application.Query.GetOrder;

// Record inmutable: una vez creada la query, nadie puede modificarla
public sealed record GetOrderQuery(Guid OrderId) : IRequest<Result<GetOrderQueryResponse>>;
```

> **¿Por qué un record?** Los records son inmutables por valor. Garantizan que la query no se modifica durante su recorrido por el pipeline de MediatR. También generan automáticamente `Equals`, `GetHashCode` y `ToString`, útiles para logging y debugging.

### GetOrderQueryHandler.cs

```csharp
// SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs
public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Result<GetOrderQueryResponse>>
{
    private readonly IDbConnection _connection;

    public GetOrderQueryHandler(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Result<GetOrderQueryResponse>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        // SQL directo con Dapper — sin EF Core overhead
        const string sql = @"SELECT Id, CustomerId, Status FROM Orders WHERE Id = @OrderId";
        var order = await _connection.QueryFirstOrDefaultAsync<OrderDto>(sql, new { request.OrderId });

        if (order is null)
            return Result<GetOrderQueryResponse>.Failure(DomainErrors.Order.NotFound(request.OrderId));

        return Result<GetOrderQueryResponse>.Success(new GetOrderQueryResponse { Order = order });
    }
}
```

> **¿Por qué el handler usa Dapper y no EF Core?** En el lado de lectura queremos velocidad y control total del SQL. Dapper mapea directo a DTOs sin el overhead del change tracker. Si el SQL cambia, solo cambia este handler, sin afectar a ningún otro slice.

---

## Slice de Command: CreateOrder

Un slice de **escritura** es más rico: valida reglas de negocio, crea agregados, y puede disparar domain events.

### CreateOrderCommand.cs

```csharp
// SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommand.cs
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
```

### CreateOrderCommandHandler.cs

```csharp
// SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommandHandler.cs
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderWriteRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(IOrderWriteRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar y construir value objects
        var addressResult = Address.Create(request.ShippingStreet, request.ShippingCity,
            request.ShippingState, request.ShippingPostalCode, request.ShippingCountry);
        if (!addressResult.IsSuccess)
            return Result<Guid>.Failure(addressResult.Error!);

        // 2. Crear el agregado (dispara domain events internamente)
        var order = Order.Create(OrderId.Create(), customerIdResult.Value!, addressResult.Value!);

        // 3. Persistir a través del Unit of Work (transaccional)
        await _orderRepository.AddAsync(order.Value!, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);  // ← Domain events se despachan aquí

        return Result<Guid>.Success(order.Value!.Id.Value);
    }
}
```

---

## Cómo añadir un nuevo Slice

Cuando necesites una nueva funcionalidad (por ejemplo, "Cancelar una orden"), sigue estos pasos:

### 1. Crear la carpeta del slice

```
SoftwareLearningGuide.Application.Command/
└── CancelOrder/          ← Nueva carpeta
    ├── CancelOrderCommand.cs
    ├── CancelOrderCommandHandler.cs
    └── CancelOrderCommandResponse.cs
```

### 2. Definir el Command

```csharp
namespace SoftwareLearningGuide.Application.Command.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result<bool>>;
```

### 3. Implementar el Handler

```csharp
namespace SoftwareLearningGuide.Application.Command.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    private readonly IOrderWriteRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(IOrderWriteRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<bool>.Failure(DomainErrors.Order.NotFound(request.OrderId));

        var result = order.Cancel(request.Reason);
        if (!result.IsSuccess)
            return Result<bool>.Failure(result.Error!);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
```

### 4. Registrar el endpoint

MediatR descubre los handlers automáticamente por assembly. **No necesitas registrar nada más** en el startup. Solo añade el endpoint en el controller:

```csharp
[HttpDelete("{id:guid}")]
public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason, CancellationToken ct)
{
    var result = await _mediator.Send(new CancelOrderCommand(id, reason), ct);
    return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
}
```

> **Clave del Vertical Slicing:** Para añadir `CancelOrder`, tocaste **exactamente** 2 archivos: el nuevo handler y el controller. Sin modificar nada más. Sin romper otros slices. Esto es el Open/Closed Principle en su máxima expresión.

---

## Diagrama de Arquitectura

```
HTTP Request
     │
     ▼
┌─────────────────────────────────────────────────────────────────────┐
│  SoftwareLearningGuide.Api (Controller)                             │
│  Solo despacha: _mediator.Send(new CreateOrderCommand { ... })      │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│  MediatR (Dispatcher)                                               │
│  Resuelve el handler correcto por tipo de request                   │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        │                       │                       │
        ▼                       ▼                       ▼
┌──────────────┐    ┌───────────────────┐    ┌──────────────────┐
│  GetOrder/   │    │   CreateOrder/    │    │  CreateProduct/  │
│              │    │                   │    │                  │
│  Query.cs    │    │  Command.cs       │    │  Command.cs      │
│  Handler.cs  │    │  Handler.cs       │    │  Handler.cs      │
│  Response.cs │    │  Response.cs      │    │  Response.cs     │
│              │    │                   │    │                  │
│  ← Slice 1 → │    │   ← Slice 2 →     │    │  ← Slice 3 →     │
└──────┬───────┘    └────────┬──────────┘    └───────┬──────────┘
       │                     │                       │
       ▼                     ▼                       ▼
┌──────────────┐    ┌───────────────────┐    ┌──────────────────┐
│   Dapper     │    │     EF Core       │    │    EF Core       │
│  (lectura)   │    │  + UnitOfWork     │    │  + UnitOfWork    │
│  SQL directo │    │  (escritura)      │    │  (escritura)     │
└──────────────┘    └───────────────────┘    └──────────────────┘
                             │
                             ▼
                    ┌────────────────┐
                    │   Ports/       │
                    │  Interfaces    │
                    │ compartidas    │
                    │ (IUnitOfWork,  │
                    │  IRepository)  │
                    └────────────────┘
```

---

## Beneficios y Trade-offs

### ✅ Beneficios

| Beneficio | Descripción |
|-----------|-------------|
| **Alta cohesión** | Todo lo de una feature está junto, sin saltar entre carpetas |
| **Bajo acoplamiento** | Los slices no se conocen entre sí |
| **Open/Closed** | Añadir features no modifica código existente |
| **Testabilidad** | Cada slice se puede testear de forma completamente aislada |
| **Equipos paralelos** | Múltiples developers pueden trabajar en features distintas sin conflictos de merge |
| **Refactoring seguro** | Cambiar la implementación de un slice no afecta a otros |

### ⚠️ Trade-offs

| Trade-off | Descripción |
|-----------|-------------|
| **Duplicación controlada** | Dos slices pueden tener código similar (validaciones, mapeos). Se acepta por la autonomía que aporta |
| **Convención de nombres** | Requiere disciplina para nombrar los archivos de forma consistente |
| **Curva inicial** | Developers acostumbrados a Layer Architecture necesitan un cambio de mentalidad |

> **¿Cuándo NO usar Vertical Slicing?** En aplicaciones CRUD muy simples donde todas las features son idénticas (leer, escribir, borrar entidades sin lógica), la arquitectura por capas puede ser más directa. Vertical Slicing brilla cuando las features tienen lógica propia, diferente entre sí, y crecen en número.

---

## Referencia de Archivos

| Archivo | Slice | Descripción |
|---------|-------|-------------|
| [`Application.Query/GetOrder/GetOrderQuery.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQuery.cs) | GetOrder | Definición de la query |
| [`Application.Query/GetOrder/GetOrderQueryHandler.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs) | GetOrder | Handler con Dapper |
| [`Application.Query/GetOrder/GetOrderQueryResponse.cs`](../../SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryResponse.cs) | GetOrder | DTO de respuesta |
| [`Application.Command/CreateOrder/CreateOrderCommand.cs`](../../SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommand.cs) | CreateOrder | Definición del command |
| [`Application.Command/CreateOrder/CreateOrderCommandHandler.cs`](../../SoftwareLearningGuide.Application.Command/CreateOrder/CreateOrderCommandHandler.cs) | CreateOrder | Handler con EF Core + UoW |
| [`Application/Ports/IBaseRepository.cs`](../../SoftwareLearningGuide.Application/Ports/IBaseRepository.cs) | Compartido | Puerto genérico de repositorio |
| [`Application/Ports/IUnitOfWork.cs`](../../SoftwareLearningGuide.Application/Ports/IUnitOfWork.cs) | Compartido | Puerto de Unit of Work |

---

**Ver también:**
- [`README.CQRS.md`](README.CQRS.md) - Separación de Commands y Queries con MediatR
- [`README.CleanArchitecture.md`](../SoftwareLearningGuide.Api/README.CleanArchitecture.md) - Cómo se relacionan las capas
- [`README.Pattern.SOLID.md`](README.Pattern.SOLID.md) - Principios que hacen funcionar los slices
