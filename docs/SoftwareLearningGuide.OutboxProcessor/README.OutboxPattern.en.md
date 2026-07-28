# Outbox Pattern in SoftwareLearningGuide

![Outbox Pattern](https://img.shields.io/badge/Pattern-Transactional_Outbox-blue)
![MassTransit](https://img.shields.io/badge/Messaging-MassTransit-green)
![EF Core](https://img.shields.io/badge/ORM-EF_Core-purple)
![RabbitMQ](https://img.shields.io/badge/Broker-RabbitMQ-orange)

The **Outbox Pattern** (Transactional Outbox) solves the **dual write** problem: how to guarantee that a message is sent to messaging only if the database transaction completes successfully.

---

## Table of Contents

1. [The Problem It Solves](#the-problem-it-solves)
2. [Solution: Transactional Outbox](#solution-transactional-outbox)
3. [Implemented Architecture](#implemented-architecture)
4. [MassTransit Configuration](#masstransit-configuration)
5. [Consumers](#consumers)
6. [Complete Step-by-Step Flow](#complete-step-by-step-flow)
7. [Docker Compose](#docker-compose)
8. [Why This Architecture is the Best Solution](#why-this-architecture-is-the-best-solution)

---

## The Problem It Solves

### Dual Write (the classic problem)

```
1. Save to SQL Server          → OK
2. Publish to RabbitMQ         → ERROR (RabbitMQ down)
```

**Result**: The order was saved but the message was never sent. There is no atomicity between database and messaging.

### Naive attempt: SQL + RabbitMQ in parallel

```
1. Publish to RabbitMQ         → OK
2. Save to SQL Server          → ERROR (constraint violation)
```

**Result**: The message was sent but the order was not saved. Other systems believe there is an order that doesn't exist.

Both scenarios create **data inconsistencies** that are extremely difficult to detect and correct in production.

---

## Solution: Transactional Outbox

The **Transactional Outbox** solves this by saving the message **within the same SQL transaction** as the entity. Instead of relying on MassTransit's built-in outbox (`EntityFrameworkOutbox`), this implementation uses a custom approach with Dapper and a dedicated worker:

```
BEGIN TRANSACTION
  1. INSERT INTO Orders (...)
  2. INSERT INTO DomainOutboxMessages (...)
COMMIT  ← ATOMIC: both or neither
```

An independent processing service (`OutboxProcessor`) periodically reads the `DomainOutboxMessages` table and publishes messages to RabbitMQ.

> **Think of the Outbox as a mailbox in an office building.** When you want to send a letter, you put it in the mailbox (`DomainOutboxMessages`) along with the signed document (the entity in the DB). The mail carrier (`OutboxProcessor`) comes periodically, picks up the mail from the mailbox, and delivers it. If the carrier can't deliver one, the letter stays in the mailbox until it can. The key is that the letter and the signed document are saved at the same time (same transaction), so you never lose one without the other.

### Guarantees

| Guarantee | How It's Achieved |
|----------|---------------|
| **Atomicity** | Message and entity in the same SQL transaction |
| **No message loss** | If RabbitMQ is down, the message stays in DomainOutboxMessages |
| **At-least-once delivery** | OutboxProcessor retries until the broker confirms |
| **No duplicates** | Consumers must be idempotent (handle repetitions) |

---

## Implemented Architecture

### General Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    Main API (ASP.NET Core)                   │
│                                                             │
│  CommandHandler                                             │
│  ├── new Order(...)        → AddDomainEvent(...)            │
│  ├── _repository.Add(order)                                 │
│  └── _unitOfWork.SaveChangesAsync()                         │
│      │                                                      │
│      ▼                                                      │
│  UnitOfWork.DispatchDomainEventsAsync()                     │
│  ├── IMediator.Publish(domainEvent)                         │
│  │   └── NotificationHandler → IOutboxWriter.WriteAsync()   │
│  │       └── Saves message in DomainOutboxMessages          │
│  └── _context.SaveChangesAsync()                            │
│      │                                                      │
│      ▼                                                      │
│  SQL Server Transaction                                     │
│  INSERT INTO Orders (…)                                     │
│  INSERT INTO OrderLines (…)                                 │
│  INSERT INTO DomainOutboxMessages (Content, Type, ...)      │
│  COMMIT                                                     │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌───────────────────────────────────────────────────────────────────────┐
│              OutboxProcessor (Worker Service)                         │
│                                                                       │
│  CustomOutboxProcessorWorker                                          │
│  ├── QueryDelay = 5 seconds                                           │
│  ├── SELECT * FROM DomainOutboxMessages WHERE ProcessedOnUtc IS NULL  │
│  ├── Deserializes and Publishes to RabbitMQ via IPublishEndpoint      │
│  └── UPDATE DomainOutboxMessages SET ProcessedOnUtc = ...             │
└───────────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                    RabbitMQ (Message Broker)                │
│                                                             │
│  Queues:                                                    │
│  ├── order-created          ← OrderCreatedConsumer          │
│  ├── order-cancelled        ← OrderCancelledConsumer        │
│  ├── product-created        ← ProductCreatedConsumer        │
│  └── product-stock-low      ← ProductStockLowConsumer       │ 
└─────────────────────────────────────────────────────────────┘
```

### Projects Involved

| Project | Role |
|----------|-----|
| `Core.Business` | Domain Events + Aggregates |
| `Application.Command` | Notification Handlers + IOutboxWriter |
| `Infraestructure` | UnitOfWork + IOutboxWriter + OutboxWriter |
| `Infraestructure.Data` | DomainOutboxMessages + OutboxMessageEntity config |
| `OutboxProcessor` | Worker Service with Dapper that processes the Outbox |
| `Consumer` | MassTransit consumer that listens to RabbitMQ |
| `Api` | REST API that generates entities and events |

---

## MassTransit Configuration

### In the Main API (`Infraestructure/Dependencies.cs`)

The API does **NOT** use MassTransit to publish events directly. Instead, it uses a custom `IOutboxWriter` that persists integration events in the `DomainOutboxMessages` table as part of the current SQL transaction. This ensures the message is saved atomically with the domain entity.

```csharp
services.AddDbContext<ApplicationDbContext>(options => {
    options.UseSqlServer(connectionString);
});

services.AddScoped<IOutboxWriter, OutboxWriter>();
```

[`SoftwareLearningGuide.Infraestructure\Dependencies.cs`](SoftwareLearningGuide.Infraestructure\Dependencies.cs)

**Key points:**

- `IOutboxWriter` is the interface defining `WriteAsync<T>()` — any domain handler can invoke it to queue an integration message.
- `OutboxWriter` (implementation) serializes the event as JSON and inserts it as a row in `DomainOutboxMessages` using the same `ApplicationDbContext`.
- MassTransit is not registered in the API — the infrastructure layer has no dependency on the message broker.

[`SoftwareLearningGuide.Infraestructure\Services\OutboxWriter.cs`](SoftwareLearningGuide.Infraestructure\Services\OutboxWriter.cs) | [`SoftwareLearningGuide.Application.Command\Ports\IOutboxWriter.cs`](SoftwareLearningGuide.Application.Command\Ports\IOutboxWriter.cs)

### In the OutboxProcessor (`Program.cs`)

The OutboxProcessor is a Worker Service hosted as an independent process. It does NOT use MassTransit's `EntityFrameworkOutbox` or `AddEntityFrameworkOutbox`. Instead, it uses Dapper to query the `DomainOutboxMessages` table directly and `IPublishEndpoint` from MassTransit to publish deserialized messages to RabbitMQ.

[`SoftwareLearningGuide.OutboxProcessor\Program.cs`](SoftwareLearningGuide.OutboxProcessor\Program.cs) | [`SoftwareLearningGuide.OutboxProcessor\Workers\CustomOutboxProcessorWorker.cs`](SoftwareLearningGuide.OutboxProcessor\Workers\CustomOutboxProcessorWorker.cs)

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions(builder.Configuration);

var databaseOptions = builder.Configuration.GetDatabaseOptions();
var messageBrokerOptions = builder.Configuration.GetMessageBrokerOptions();
var otelOptions = builder.Configuration.GetOpenTelemetryOptions();

builder.Services.AddCustomOpenTelemetry(builder.Logging, builder.Configuration, otelOptions);

builder.Services.AddSingleton<IDbConnection>(_ => new SqlConnection(databaseOptions.SoftwareLearningGuide));

builder.Services.AddMassTransit(x => {
    x.UsingRabbitMq((context, cfg) => {
        cfg.Host(messageBrokerOptions.Host, "/", h => {
            h.Username(messageBrokerOptions.Username);
            h.Password(messageBrokerOptions.Password);
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddHostedService<CustomOutboxProcessorWorker>();

var host = builder.Build();
host.Run();
```

**Key points:**

- `IDbConnection` (Dapper) is registered as a singleton for direct queries to `DomainOutboxMessages`.
- MassTransit is configured **only for publishing** (`IPublishEndpoint`), not consuming. There is no `AddConsumer`, `AddEntityFrameworkOutbox`, `QueryDelay` or `UseBusOutbox` in this configuration.
- The `CustomOutboxProcessorWorker` worker runs as a `BackgroundService` and manages the read/publish/acknowledge cycle.
- `ConfigureEndpoints()` is present but only used for MassTransit's publish endpoint configuration, not for consuming queues.

[`SoftwareLearningGuide.OutboxProcessor\Options\DatabaseOptions.cs`](SoftwareLearningGuide.OutboxProcessor/Options/DatabaseOptions.cs) | [`SoftwareLearningGuide.OutboxProcessor\Options\MessageBrokerOptions.cs`](SoftwareLearningGuide.OutboxProcessor/Options/MessageBrokerOptions.cs)

> **Why do the API and OutboxProcessor have different MassTransit configurations?** Because the API only **WRITES** to the outbox (via `IOutboxWriter`), while the OutboxProcessor only **READS** from the outbox and **PUBLISHES** to RabbitMQ. They have opposite roles: one is a message producer, the other is the intermediary that delivers them. The API doesn't need consumers or consumption endpoints; the OutboxProcessor doesn't need `EntityFrameworkOutbox` because it manages outbox reading with Dapper in a custom way.

---

## Consumers

### OrderCreatedConsumer

Unlike other consumers that run within the OutboxProcessor, `OrderCreatedConsumer` belongs to the `SoftwareLearningGuide.Consumer` project, which is an independent consumer service. It listens to the `order-created` queue in RabbitMQ and processes the `OrderCreatedEvent` (not `OrderCreatedIntegrationEvent` as previously named).

[`SoftwareLearningGuide.Consumer\Consumers\OrderCreatedConsumer.cs`](SoftwareLearningGuide.Consumer/Consumers/OrderCreatedConsumer.cs)

```csharp
public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent> {
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[OrderCreatedConsumer] Order created: OrderId={OrderId}, CustomerId={CustomerId}, Total={TotalAmount} {Currency}",
            message.OrderId,
            message.CustomerId,
            message.TotalAmount,
            message.Currency);

        await Task.CompletedTask;
    }
}
```

### OrderCancelledConsumer

[`SoftwareLearningGuide.Consumer\Consumers\OrderCancelledConsumer.cs`](SoftwareLearningGuide.Consumer/Consumers/OrderCancelledConsumer.cs)

```csharp
public sealed class OrderCancelledConsumer : IConsumer<OrderCancelledEvent> {
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(ILogger<OrderCancelledConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[OrderCancelledConsumer] Order cancelled: OrderId={OrderId}, Reason={Reason}",
            message.OrderId,
            message.Reason);

        await Task.CompletedTask;
    }
}
```

### ProductStockLowConsumer

[`SoftwareLearningGuide.Consumer\Consumers\ProductStockLowConsumer.cs`](SoftwareLearningGuide.Consumer/Consumers/ProductStockLowConsumer.cs)

```csharp
public sealed class ProductStockLowConsumer : IConsumer<ProductStockLowEvent> {
    private readonly ILogger<ProductStockLowConsumer> _logger;

    public ProductStockLowConsumer(ILogger<ProductStockLowConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductStockLowEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[ProductStockLowConsumer] Low stock alert: ProductId={ProductId}, CurrentStock={CurrentStock}",
            message.ProductId,
            message.CurrentStock);

        await Task.CompletedTask;
    }
}
```

### ProductCreatedConsumer (new)

This consumer was recently added and didn't appear in previous versions of the README. It processes the `ProductCreatedEvent` when a new product is created.

[`SoftwareLearningGuide.Consumer\Consumers\ProductCreatedConsumer.cs`](SoftwareLearningGuide.Consumer/Consumers/ProductCreatedConsumer.cs)

```csharp
public sealed class ProductCreatedConsumer : IConsumer<ProductCreatedEvent> {
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(ILogger<ProductCreatedConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[ProductCreatedConsumer] Product created: ProductId={ProductId}, Name={Name}, Price={Price}",
            message.ProductId,
            message.Name,
            message.Price);

        await Task.CompletedTask;
    }
}
```

> **Consumers are where external business logic executes.** Each consumer is responsible for one event type: when an `OrderCreatedEvent` arrives, an email is sent; when a `ProductStockLowEvent` arrives, a purchase order is created. Consumers don't know who created the event; they only process what they receive. This makes the system extensible: you can add a new consumer (e.g., `OrderCreatedAnalyticsConsumer`) without modifying anything in the API or the domain.

### Registered Consumers

Consumers are registered in `SoftwareLearningGuide.Consumer\Program.cs` (not in the OutboxProcessor):

[`SoftwareLearningGuide.Consumer\Program.cs`](SoftwareLearningGuide.Consumer/Program.cs)

| Consumer | Event | RabbitMQ Queue | Usage |
|----------|--------|---------------|-----|
| `OrderCreatedConsumer` | `OrderCreatedEvent` | `order-created` | Notifications, email, inventory |
| `OrderCancelledConsumer` | `OrderCancelledEvent` | `order-cancelled` | Reverse inventory, notify |
| `ProductCreatedConsumer` | `ProductCreatedEvent` | `product-created` | New product notifications |
| `ProductStockLowConsumer` | `ProductStockLowEvent` | `product-stock-low` | Restocking alerts |

---

## Complete Step-by-Step Flow

### Scenario: Create an Order

```
1. POST /api/v1/order
   └── CreateOrderCommandHandler.Handle()
       ├── new Order(orderId, customerId, address)
       │   └──► AddDomainEvent(OrderCreatedDomainEvent)  [in memory]
       ├── order.AddProduct(product, quantity)
       ├── order.Confirm()
       ├── _orderRepository.AddAsync(order)
       └── _unitOfWork.SaveChangesAsync()
           │
           ▼
2. UnitOfWork.DispatchDomainEventsAsync()
   ├── Finds ProduceEvents entities with pending events
   ├── IMediator.Publish(domainEvent)
   │   └── OrderCreatedNotificationHandler.Handle()
   │       └── IOutboxWriter.WriteAsync(OrderCreatedEvent)  ← Saves to DomainOutboxMessages
   └── _context.SaveChangesAsync()
       │
       ▼
3. SQL Transaction (atomic)
   ├── INSERT INTO Orders (...)
   ├── INSERT INTO OrderLines (...)
   ├── INSERT INTO DomainOutboxMessages (Type='OrderCreatedEvent', Content=...)
   └── COMMIT  ← ATOMIC
       │
       ▼
4. OutboxProcessor (every 5 seconds)
   ├── Reads DomainOutboxMessages via Dapper WHERE ProcessedOnUtc IS NULL
   ├── Deserializes Content JSON to known type (OrderCreatedEvent)
   ├── Publishes OrderCreatedEvent to RabbitMQ via IPublishEndpoint
   ├── UPDATE DomainOutboxMessages SET ProcessedOnUtc = GETUTCDATE()
   └── Marks message as processed by deleting it
       │
       ▼
5. OrderCreatedConsumer (in Consumer service)
   └── Consumes message from RabbitMQ queue order-created
       └── Executes external business logic
```

[`SoftwareLearningGuide.Infraestructure\Repositories\UnitOfWork.cs`](SoftwareLearningGuide.Infraestructure/Repositories/UnitOfWork.cs) | [`SoftwareLearningGuide.OutboxProcessor\Workers\CustomOutboxProcessorWorker.cs`](SoftwareLearningGuide.OutboxProcessor/Workers/CustomOutboxProcessorWorker.cs) | [`SoftwareLearningGuide.Application.Command\NotificationHandlers\OrderCreatedNotificationHandler.cs`](SoftwareLearningGuide.Application.Command/NotificationHandlers/OrderCreatedNotificationHandler.cs)

> **Why so many steps?** Because each step solves a specific problem: the domain validates rules, the `UnitOfWork` ensures atomicity, the Outbox prevents message loss, and the Consumer executes external logic. If we remove any step, we lose a guarantee. For example, if we remove the Outbox, we return to the "dual write" problem. If we remove the UnitOfWork, we wouldn't have atomicity between the entity and the outbox.

### DomainOutboxMessages Table

| Id | Type | Content | CreatedOnUtc | ProcessedOnUtc | Error |
|----|------|---------|--------------|----------------|-------|
| 1 | `OrderCreatedEvent` | `{OrderId: "...", CustomerId: "...", ...}` | 2026-01-15 10:30:00 | NULL | NULL |

[`SoftwareLearningGuide.Infraestructure.Data\Entities\OutboxMessageEntity.cs`](SoftwareLearningGuide.Infraestructure.Data/Entities/OutboxMessageEntity.cs) | [`SoftwareLearningGuide.Infraestructure.Data\Configurations\OutboxMessageConfiguration.cs`](SoftwareLearningGuide.Infraestructure.Data/Configurations/OutboxMessageConfiguration.cs)

**Note on table configuration:**

- The table is mapped as `DomainOutboxMessages` (not `OutboxMessage`) via `ToTable("DomainOutboxMessages")`.
- The `Type` field stores the C# type name of the integration event (e.g., `OrderCreatedEvent`).
- The `Content` field stores the serialized JSON of the event.
- The composite index `(ProcessedOnUtc, CreatedOnUtc)` optimizes OutboxProcessor queries to select pending messages in chronological order.
- The original MassTransit migration (`OutboxMessage`, `InboxState`, `OutboxState`) was replaced by the custom `DomainOutboxMessages` table in the `AddDomainOutboxMessagesTable` migration (`SoftwareLearningGuide.Infraestructure.Data\Migrations\20260726173737_AddDomainOutboxMessagesTable.cs`).

---

## Docker Compose

The stack includes RabbitMQ and the OutboxProcessor:

### Added Services

| Service | Port | Description |
|----------|--------|-------------|
| **RabbitMQ** | 5672 (AMQP), 15672 (Management) | Message broker |
| **OutboxProcessor** | — | Worker service that processes the Outbox |
| **Consumer** | — | Event consumption service |

### Starting the Stack

```bash
# Start everything (API + SQL + RabbitMQ + OutboxProcessor + Consumer)
docker-compose up -d

# View OutboxProcessor logs
docker-compose logs -f outbox-processor

# View Consumer logs
docker-compose logs -f consumer

# View RabbitMQ logs
docker-compose logs -f rabbitmq

# Access RabbitMQ Management UI
# http://localhost:15672 (guest/guest)
```

### OutboxProcessor Environment Variables

Actual values are read from `appsettings.json` and `appsettings.Development.json`:

[`SoftwareLearningGuide.OutboxProcessor\appsettings.Development.json`](SoftwareLearningGuide.OutboxProcessor/appsettings.Development.json)

```yaml
outbox-processor:
  environment:
    - Database__SoftwareLearningGuide=Server=sqlserver;Database=SoftwareLearningGuide;...
    - MessageBroker__Host=rabbitmq
    - MessageBroker__Username=guest
    - MessageBroker__Password=guest
  depends_on:
    - sqlserver
    - rabbitmq
```

**Note:** The OutboxProcessor now uses configuration options (`DatabaseOptions`, `MessageBrokerOptions`, `OpenTelemetryOptions`) instead of hardcoded environment variables. This allows configuring values per environment without modifying the code.

[`SoftwareLearningGuide.OutboxProcessor\Options\DatabaseOptions.cs`](SoftwareLearningGuide.OutboxProcessor/Options/DatabaseOptions.cs) | [`SoftwareLearningGuide.OutboxProcessor\Options\MessageBrokerOptions.cs`](SoftwareLearningGuide.OutboxProcessor/Options/MessageBrokerOptions.cs)

---

## Why This Architecture is the Best Solution

### 1. Guaranteed Atomicity (ACID)

The message and entity are saved in the **same SQL transaction**. If the transaction fails, no message is generated in `DomainOutboxMessages`. There is no inconsistent state.

> **What happens if the SQL transaction fails after the INSERT but before the COMMIT?** Nothing is saved: neither the entity nor the message. The app can retry the entire operation without risk of duplicates. Without the Outbox, the message could have been sent to RabbitMQ while the entity wasn't saved, creating a silent inconsistency.

### 2. No Message Loss

If RabbitMQ is down, messages remain in the `DomainOutboxMessages` table. When the broker recovers, the OutboxProcessor processes them automatically.

> **What happens if RabbitMQ goes down while the OutboxProcessor is processing?** Messages that were already published are lost (unless the consumer fails and RabbitMQ requeues them). Messages still in `DomainOutboxMessages` remain pending and are processed when the broker recovers. The key is that the OutboxProcessor automatically retries: no manual intervention needed.

### 3. Total Decoupling

The API doesn't know who consumes the events. You can add consumers in other microservices without modifying the API. The domain only generates events; it doesn't care who consumes them.

> **Concrete example:** Imagine you need to add a consumer that sends an SMS when an order is created. You just need to create `OrderCreatedSmsConsumer` and register it in the Consumer service. The API is not modified, the domain is not modified, and the existing consumer continues to work. It's like adding a new recipient in the mailbox: the letter is already written, only who receives it changes.

### 4. Scalability

You can add more `NotificationHandlers` or `Consumers` without modifying the domain. The OutboxProcessor can scale horizontally (with optimistic locking on the table via the `ProcessedOnUtc` field).

> **Concrete example:** If your system processes 10,000 orders per minute, you can run 3 instances of the OutboxProcessor. Each queries `DomainOutboxMessages` with an optimistic filter (`ProcessedOnUtc IS NULL`) so they don't process the same message twice. The `DomainOutboxMessages` table acts as a persistent queue that survives restarts and crashes.

### 5. Observability

Each message in `DomainOutboxMessages` has a unique `Id` that allows correlating the entire flow from creation to processing by the final consumer. Structured logs facilitate debugging.

> **Concrete example:** When a customer reports "I didn't receive my confirmation email", you can search for the message `Id` in the API logs (where the event was created in `DomainOutboxMessages`), in the OutboxProcessor logs (where it was published), and in the consumer logs (where it was processed). If the event doesn't appear in the table, the problem is in the transaction. If it appears but not in the consumer, the problem is in RabbitMQ.

### 6. Automatic Recovery

If the OutboxProcessor crashes, on restart it continues processing pending messages. Nothing is lost.

> **Concrete example:** If the OutboxProcessor crashes at 2 AM and restarts at 6 AM, messages that arrived between 2 and 6 are still in `DomainOutboxMessages` with `ProcessedOnUtc IS NULL`. On restart, the processor processes them in chronological order (sorted by `CreatedOnUtc`). No data loss, no duplicate messages (thanks to the `ProcessedOnUtc` marking), and no manual intervention needed. It's like if the mail carrier gets sick one day and the next day delivers all accumulated mail.

---

## Recommended Resources

- [Transactional Outbox Pattern - Microservices.io](https://microservices.io/patterns/data/transactional-outbox.html)
- [MassTransit Outbox Documentation](https://masstransit.io/documentation/configuration/persistence/entity-framework)
- [Outbox Pattern - Chris Richardson](https://microservices.io/patterns/data/transactional-outbox.html)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)

---

**Last updated:** 2026
**Project:** SoftwareLearningGuide.OutboxProcessor
