# Dapper - Query Side

![Dapper](https://img.shields.io/badge/Library-Dapper-green)
![CQRS](https://img.shields.io/badge/Pattern-Query_Side-blue)

**Dapper** es un ORM micro que mapea SQL directo a objetos C#. Se usa en el lado de **Query (lectura)** por su velocidad y control total sobre las queries.

---

## Tabla de Contenidos

1. [¿Por qué Dapper para Queries?](#por-qué-dapper-para-queries)
2. [IDbConnection y Configuración](#idbconnection-y-configuración)
3. [Queries Básicas](#queries-básicas)
4. [CommandDefinition](#commanddefinition)
5. [Parámetros Anónimos](#parámetros-anónimos)
6. [Mapeo de Resultados](#mapeo-de-resultados)
7. [Dapper vs EF Core](#dapper-vs-ef-core)
8. [Referencia de Archivos](#referencia-de-archivos)

---

## ¿Por qué Dapper para Queries?

| Ventaja | Descripción |
|---------|-------------|
| **Velocidad** | Más rápido que EF Core sin tracking |
| **SQL controlado** | Escribes exactamente qué se ejecuta |
| **Sin tracking** | No mantiene estado de entidades (lectura pura) |
| **Join directo** | Puedes hacer joins complejos sin include paths |
| **Proyección** | Mapea directamente a DTOs, no a entidades del dominio |

### Contexto CQRS: por qué Dapper aquí y no en commands

En arquitectura CQRS, el lado de **lectura** (queries) y el lado de **escritura** (commands) tienen diferentes necesidades. Los commands necesitan tracking, validación y transaccionalidad — ahí brilla EF Core. Las queries necesitan **velocidad** y **control total** del SQL — ahí brilla Dapper. Por eso usamos cada herramienta para lo que mejor hace. Dapper no tiene tracking porque no lo necesitas: solo estás leyendo datos, no modificándolos.

---

## IDbConnection y Configuración

El handler recibe un `IDbConnection` por inyección de dependencias y usa `DomainErrors` para mensajes de error consistentes:

> **Archivo productivo:** [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs`](SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs)

```csharp
// Application.Query/GetOrder/GetOrderQueryHandler.cs
using Dapper;
using MediatR;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;

public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Result<GetOrderQueryResponse>>
{
    private readonly IDbConnection _connection;

    public GetOrderQueryHandler(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}
```

En `Program.cs` se registra la conexión:

```csharp
builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## Queries Básicas

### QueryFirstOrDefaultAsync

Devuelve un solo registro o `null`:

> **Archivo productivo:** [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs`](SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs)

```csharp
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

var command = new CommandDefinition(orderSql, new { request.OrderId }, cancellationToken: cancellationToken);
var order = await _connection.QueryFirstOrDefaultAsync<OrderDto>(command);

if (order is null)
    return Result<GetOrderQueryResponse>.Failure(
        DomainErrors.Order.NotFound(request.OrderId));  // ← Usa DomainErrors, no string inline
```

### QueryAsync

Devuelve una lista de registros:

```csharp
const string linesSql = @"
    SELECT
        ol.Id, ol.ProductId, ol.ProductName,
        ol.UnitPrice, ol.Currency, ol.Quantity,
        (ol.UnitPrice * ol.Quantity) AS Subtotal
    FROM OrderLines ol
    WHERE ol.OrderId = @OrderId;";

var command = new CommandDefinition(linesSql, new { request.OrderId }, cancellationToken: cancellationToken);
var lines = (await _connection.QueryAsync<OrderLineDto>(command)).ToList();
```

### ¿Por qué dos queries separadas?

¿Por qué no hacer un solo query con JOIN entre Orders y OrderLines? Podrías, pero aquí separamos la query de la orden de la query de las líneas por **claridad** y porque Dapper maneja mejor queries simples. Cada query tiene una responsabilidad: una busca la orden, otra busca sus líneas. En producción, si la performance es crítica (por ejemplo, con miles de líneas por orden), podrías combinarlas en un solo query con JOIN para reducir round-trips a la base de datos.

---

## CommandDefinition

`CommandDefinition` encapsula SQL, parámetros y configuración:

```csharp
var command = new CommandDefinition(
    sql,                              // SQL a ejecutar
    new { request.OrderId },          // Parámetros anónimos
    cancellationToken: cancellationToken); // CancellationToken

var result = await _connection.QueryFirstOrDefaultAsync<OrderDto>(command);
```

### Ventajas de CommandDefinition

| Característica | Descripción |
|----------------|-------------|
| **CancellationToken** | Soporte nativo de cancelación |
| **Parámetros tipados** | Type-safe con objetos anónimos |
| **CommandTimeout** | Timeout personalizado por query |
| **Transaction** | Soporte de transacciones |

`CommandDefinition` encapsula todo lo que necesitas para ejecutar un query: el SQL, los parámetros, el token de cancelación y opcionalmente un timeout. Es como un paquete completo que le das a Dapper y él se encarga de ejecutarlo de forma segura. A diferencia de pasar parámetros sueltos a los métodos de Dapper (`QueryAsync<T>(sql, param)`), `CommandDefinition` te da soporte nativo de cancelación y timeout — algo crítico en producción donde una query lenta no debería bloquear el thread infinitamente.

---

## Parámetros Anónimos

Los parámetros se pasan como objetos anónimos:

```csharp
// Parámetro simple
new { request.OrderId }

// Múltiples parámetros
new { CustomerId = customerId, Status = "Pending" }

// Con valores
new { MinPrice = 100m, MaxPrice = 500m, Currency = "USD" }
```

Dapper los convierte automáticamente a parámetros SQL de forma segura (sin SQL injection).

---

## Mapeo de Resultados

Dapper mapea automáticamente las columnas del SQL a las propiedades del DTO:

```csharp
// SQL devuelve: Id, CustomerId, CustomerName, Status, ...
// OrderDto tiene: Id, CustomerId, CustomerName, Status, ...
// Dapper mapea por nombre de columna → propiedad

public sealed record OrderDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    // ...
}
```

### Alias en SQL

Cuando el nombre de la columna no coincide con la propiedad:

```sql
SELECT
    c.FirstName + ' ' + c.LastName AS CustomerName,  -- Alias
    c.Email AS CustomerEmail                          -- Alias
FROM Customers c
```

---

## Dapper vs EF Core

| Característica | Dapper | EF Core |
|----------------|--------|---------|
| **Velocidad (lectura)** | Más rápido | Más lento |
| **Velocidad (escritura)** | Manual | Tracking automático |
| **SQL control** | Total | Generado por el ORM |
| **Tracking** | No | Sí |
| **Migraciones** | No | Sí |
| **Relaciones** | Join manual | Include/ThenInclude |
| **Uso en CQRS** | Query side | Command side |

### ¿Cuándo usar cada uno?

La regla general en CQRS: usa **Dapper para queries** (lectura) porque es más rápido y te da control total del SQL. Usa **EF Core para commands** (escritura) porque maneja tracking, relaciones y migraciones automáticamente. No es una competencia entre ORM y micro-ORM; son herramientas complementarias que resuelven problemas distintos. Dapper no tiene `Include()`, `SaveChanges()` ni migraciones — y no las necesita. EF Core no tiene `Query<T>()` con SQL puro ni mapeo directo a DTOs tan eficiente — y para eso está Dapper.

---

## Flujo Completo de un Query

```
1. Controller recibe GET /api/v1/order/{id}
2. Controller crea new GetOrderQuery(orderId)
3. Controller llama a _mediator.Send(query)
4. MediatR resuelve GetOrderQueryHandler
5. Handler ejecuta SQL con Dapper
6. Handler mapea resultado a OrderDto
7. Handler retorna Result<GetOrderQueryResponse>.Success(...)
8. Controller retorna Ok(result.Value)
```

---

## Referencia de Archivos

| Archivo | Descripción |
|---------|-------------|
| `Application.Query/GetOrder/GetOrderQuery.cs` | Definición del query |
| `Application.Query/GetOrder/GetOrderQueryHandler.cs` | Handler con Dapper |
| `Application.Query/GetOrder/OrderDto.cs` | DTOs de lectura |
| `Application.Query/GetOrder/GetOrderQueryResponse.cs` | Response wrapper |

---

## Paquetes NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Dapper` | 2.1.66 | Mapeo SQL → objetos |
| `Microsoft.Data.SqlClient` | 5.2.2 | Conexión a SQL Server |
