# Dapper - Query Side

![Dapper](https://img.shields.io/badge/Library-Dapper-green)
![CQRS](https://img.shields.io/badge/Pattern-Query_Side-blue)

**Dapper** is a micro ORM that maps direct SQL to C# objects. It's used on the **Query (read)** side for its speed and total control over queries.

---

## Table of Contents

1. [Why Dapper for Queries?](#why-dapper-for-queries)
2. [IDbConnection and Configuration](#idbconnection-and-configuration)
3. [Basic Queries](#basic-queries)
4. [CommandDefinition](#commanddefinition)
5. [Anonymous Parameters](#anonymous-parameters)
6. [Result Mapping](#result-mapping)
7. [Dapper vs EF Core](#dapper-vs-ef-core)
8. [File Reference](#file-reference)

---

## Why Dapper for Queries?

| Advantage | Description |
|-----------|-------------|
| **Speed** | Faster than EF Core without tracking |
| **Controlled SQL** | You write exactly what executes |
| **No tracking** | Doesn't maintain entity state (pure reads) |
| **Direct joins** | You can do complex joins without include paths |
| **Projection** | Maps directly to DTOs, not domain entities |

### CQRS Context: why Dapper here and not in commands

In CQRS architecture, the **read** side (queries) and **write** side (commands) have different needs. Commands need tracking, validation, and transactionality — that's where EF Core shines. Queries need **speed** and **total SQL control** — that's where Dapper shines. That's why we use each tool for what it does best. Dapper doesn't have tracking because you don't need it: you're only reading data, not modifying it.

---

## IDbConnection and Configuration

The handler receives an `IDbConnection` via dependency injection and uses `DomainErrors` for consistent error messages:

> **Production file:** [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs`](SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs)

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

In `Program.cs` the connection is registered:

```csharp
builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## Basic Queries

### QueryFirstOrDefaultAsync

Returns a single record or `null`:

> **Production file:** [`SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs`](SoftwareLearningGuide.Application.Query/GetOrder/GetOrderQueryHandler.cs)

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
        DomainErrors.Order.NotFound(request.OrderId));  // ← Uses DomainErrors, not inline string
```

### QueryAsync

Returns a list of records:

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

### Why two separate queries?

Why not do a single query with a JOIN between Orders and OrderLines? You could, but here we separate the order query from the lines query for **clarity** and because Dapper handles simple queries better. Each query has a responsibility: one finds the order, the other finds its lines. In production, if performance is critical (for example, with thousands of lines per order), you could combine them into a single JOIN query to reduce round-trips to the database.

---

## CommandDefinition

`CommandDefinition` encapsulates SQL, parameters, and configuration:

```csharp
var command = new CommandDefinition(
    sql,                              // SQL to execute
    new { request.OrderId },          // Anonymous parameters
    cancellationToken: cancellationToken); // CancellationToken

var result = await _connection.QueryFirstOrDefaultAsync<OrderDto>(command);
```

### Advantages of CommandDefinition

| Feature | Description |
|---------|-------------|
| **CancellationToken** | Native cancellation support |
| **Typed parameters** | Type-safe with anonymous objects |
| **CommandTimeout** | Custom timeout per query |
| **Transaction** | Transaction support |

`CommandDefinition` encapsulates everything you need to execute a query: the SQL, parameters, cancellation token, and optionally a timeout. It's like a complete package you give to Dapper and he handles executing it safely. Unlike passing loose parameters to Dapper's methods (`QueryAsync<T>(sql, param)`), `CommandDefinition` gives you native cancellation and timeout support — something critical in production where a slow query shouldn't block the thread indefinitely.

---

## Anonymous Parameters

Parameters are passed as anonymous objects:

```csharp
// Simple parameter
new { request.OrderId }

// Multiple parameters
new { CustomerId = customerId, Status = "Pending" }

// With values
new { MinPrice = 100m, MaxPrice = 500m, Currency = "USD" }
```

Dapper automatically converts them to safe SQL parameters (no SQL injection).

---

## Result Mapping

Dapper automatically maps SQL columns to DTO properties:

```csharp
// SQL returns: Id, CustomerId, CustomerName, Status, ...
// OrderDto has: Id, CustomerId, CustomerName, Status, ...
// Dapper maps by column name → property

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

### SQL Aliases

When the column name doesn't match the property:

```sql
SELECT
    c.FirstName + ' ' + c.LastName AS CustomerName,  -- Alias
    c.Email AS CustomerEmail                          -- Alias
FROM Customers c
```

---

## Dapper vs EF Core

| Feature | Dapper | EF Core |
|---------|--------|---------|
| **Speed (read)** | Faster | Slower |
| **Speed (write)** | Manual | Automatic tracking |
| **SQL control** | Total | Generated by ORM |
| **Tracking** | No | Yes |
| **Migrations** | No | Yes |
| **Relationships** | Manual join | Include/ThenInclude |
| **CQRS Usage** | Query side | Command side |

### When to use each?

The general rule in CQRS: use **Dapper for queries** (reads) because it's faster and gives you total SQL control. Use **EF Core for commands** (writes) because it handles tracking, relationships, and migrations automatically. It's not a competition between ORM and micro-ORM; they're complementary tools that solve different problems. Dapper doesn't have `Include()`, `SaveChanges()`, or migrations — and doesn't need them. EF Core doesn't have `Query<T>()` with pure SQL or such efficient direct DTO mapping — and that's what Dapper is for.

---

## Complete Query Flow

```
1. Controller receives GET /api/v1/order/{id}
2. Controller creates new GetOrderQuery(orderId)
3. Controller calls _mediator.Send(query)
4. MediatR resolves GetOrderQueryHandler
5. Handler executes SQL with Dapper
6. Handler maps result to OrderDto
7. Handler returns Result<GetOrderQueryResponse>.Success(...)
8. Controller returns Ok(result.Value)
```

---

## File Reference

| File | Description |
|------|-------------|
| `Application.Query/GetOrder/GetOrderQuery.cs` | Query definition |
| `Application.Query/GetOrder/GetOrderQueryHandler.cs` | Handler with Dapper |
| `Application.Query/GetOrder/OrderDto.cs` | Read DTOs |
| `Application.Query/GetOrder/GetOrderQueryResponse.cs` | Response wrapper |

---

## NuGet Packages

| Package | Version | Usage |
|---------|---------|-------|
| `Dapper` | 2.1.66 | SQL → object mapping |
| `Microsoft.Data.SqlClient` | 5.2.2 | SQL Server connection |