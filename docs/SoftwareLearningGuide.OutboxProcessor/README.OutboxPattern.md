# Outbox Pattern en SoftwareLearningGuide

![Outbox Pattern](https://img.shields.io/badge/Pattern-Transactional_Outbox-blue)
![MassTransit](https://img.shields.io/badge/Messaging-MassTransit-green)
![EF Core](https://img.shields.io/badge/ORM-EF_Core-purple)
![RabbitMQ](https://img.shields.io/badge/Broker-RabbitMQ-orange)

El **Outbox Pattern** (Patron de Bandeja de Salida) resuelve el problema de **dual escritura**: como garantizar que un mensaje se envie a la mensajeria solo si la transaccion de base de datos se completa exitosamente.

---

#### Tabla de Contenidos

1. [El Problema que Resuelve](#el-problema-que-resuelve)
2. [Solucion: Transactional Outbox](#solucion-transactional-outbox)
3. [Arquitectura Implementada](#arquitectura-implementada)
4. [Configuracion MassTransit](#configuracion-masstransit)
5. [Consumers](#consumers)
6. [Flujo Completo Paso a Paso](#flujo-completo-paso-a-paso)
7. [Docker Compose](#docker-compose)
8. [Por que esta Arquitectura es la Mejor Solucion](#por-que-esta-arquitectura-es-la-mejor-solucion)

---

## El Problema que Resuelve

### Dual Escritura (el problema clasico)

```
1. Guardar en SQL Server          → OK
2. Publicar en RabbitMQ           → ERROR (RabbitMQ caido)
```

**Resultado**: El pedido se guardo pero el mensaje nunca se envio. No hay atomicidad entre base de datos y mensajeria.

### Intento ingenuo: SQL + RabbitMQ en paralelo

```
1. Publicar en RabbitMQ           → OK
2. Guardar en SQL Server          → ERROR (constraint violation)
```

**Resultado**: El mensaje se envio pero el pedido no se guardo. Otros sistemas creen que hay un pedido que no existe.

Ambos escenarios crean **inconsistencias de datos** que son extremadamente dificiles de detectar y corregir en produccion.

---

## Solucion: Transactional Outbox

El **Transactional Outbox** resuelve esto guardando el mensaje **dentro de la misma transaccion SQL** que la entidad. En lugar de depender de MassTransit's built-outbox (`EntityFrameworkOutbox`), esta implementacion usa un enfoque personalizado con Dapper y un worker dedicado:

```
BEGIN TRANSACTION
  1. INSERT INTO Orders (...)
  2. INSERT INTO DomainOutboxMessages (...)
COMMIT  ← ATOMICO: ambos o ninguno
```

Un servicio de procesamiento independiente (`OutboxProcessor`) lee periodicamente la tabla `DomainOutboxMessages` y publica los mensajes a RabbitMQ.

> **Piensa en el Outbox como un buzón de correos en un edificio de oficinas.** Cuando quieres enviar una carta, la dejas en el buzón (`DomainOutboxMessages`) junto con el documento firmado (la entidad en la BD). El cartero (`OutboxProcessor`) viene cada cierto tiempo, recoge los correos del buzón y los entrega. Si el cartero no puede entregar uno, la carta sigue en el buzón hasta que pueda. La clave es que la carta y el documento firmado se guardan al mismo tiempo (misma transacción), así que nunca pierdes uno sin el otro.

### Garantias

| Garantia | Como se logra |
|----------|---------------|
| **Atomicidad** | Mensaje y entidad en la misma transacción SQL |
| **No perdida de mensajes** | Si RabbitMQ esta caido, el mensaje queda en DomainOutboxMessages |
| **At-least-once delivery** | OutboxProcessor reintenta hasta que el broker confirme |
| **No duplicados** | Consumers deben ser idempotentes (manejar repeticiones) |

---

## Arquitectura Implementada

### Flujo General

```
┌─────────────────────────────────────────────────────────────┐
│                    API Principal (ASP.NET Core)             │
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
│  │       └── Guarda mensaje en DomainOutboxMessages         │
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
│  ├── QueryDelay = 5 segundos                                          │
│  ├── SELECT * FROM DomainOutboxMessages WHERE ProcessedOnUtc IS NULL  │
│  ├── Deserializa y Publica a RabbitMQ via IPublishEndpoint            │
│  └── UPDATE DomainOutboxMessages SET ProcessedOnUtc = ...             │
└───────────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                    RabbitMQ (Message Broker)                │
│                                                             │
│  Colas:                                                     │
│  ├── order-created          ← OrderCreatedConsumer          │
│  ├── order-cancelled        ← OrderCancelledConsumer        │
│  ├── product-created        ← ProductCreatedConsumer        │
│  └── product-stock-low      ← ProductStockLowConsumer       │ 
└─────────────────────────────────────────────────────────────┘
```

### Proyectos Involucrados

| Proyecto | Rol |
|----------|-----|
| `Core.Business` | Domain Events + Aggregates |
| `Application.Command` | Notification Handlers + IOutboxWriter |
| `Infraestructure` | UnitOfWork + IOutboxWriter + OutboxWriter |
| `Infraestructure.Data` | DomainOutboxMessages + OutboxMessageEntity config |
| `OutboxProcessor` | Worker Service con Dapper que procesa el Outbox |
| `Consumer` | MasTransit consumer que escucha RabbitMQ |
| `Api` | API REST que genera las entidades y eventos |

---

## Configuracion MassTransit

### En la API Principal (`Infraestructure/Dependencies.cs`)

La API **NO** usa MassTransit para publicar eventos directamente. En su lugar, usa un `IOutboxWriter` personalizado que persiste los eventos de integracion en la tabla `DomainOutboxMessages` como parte de la transacción SQL actual. Esto garantiza que el mensaje se guarde atómicamente con la entidad del dominio.

```csharp
services.AddDbContext<ApplicationDbContext>(options => {
    options.UseSqlServer(connectionString);
});

services.AddScoped<IOutboxWriter, OutboxWriter>();
```

[`SoftwareLearningGuide.Infraestructure\Dependencies.cs`](SoftwareLearningGuide.Infraestructure\Dependencies.cs)

**Puntos clave:**

- `IOutboxWriter` es la interfaz que define `WriteAsync<T>()` — cualquier handler de dominio puede invocarla para encolar un mensaje de integración.
- `OutboxWriter` (implementación) serializa el evento como JSON y lo inserta como una fila en `DomainOutboxMessages` usando el mismo `ApplicationDbContext`.
- No se registra MassTransit en la API — la capa de infraestructura no tiene dependencia del broker de mensajes.

[`SoftwareLearningGuide.Infraestructure\Services\OutboxWriter.cs`](SoftwareLearningGuide.Infraestructure\Services\OutboxWriter.cs) | [`SoftwareLearningGuide.Application.Command\Ports\IOutboxWriter.cs`](SoftwareLearningGuide.Application.Command\Ports\IOutboxWriter.cs)

### En el OutboxProcessor (`Program.cs`)

El OutboxProcessor es un Worker Service hospedado como proceso independiente. NO usa MassTransit's `EntityFrameworkOutbox` ni `AddEntityFrameworkOutbox`. En su lugar, usa Dapper para consultar la tabla `DomainOutboxMessages` directamente y `IPublishEndpoint` de MassTransit para publicar los mensajes deserializados a RabbitMQ.

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

**Puntos clave:**

- `IDbConnection` (Dapper) se registra como singleton para las consultas directas a `DomainOutboxMessages`.
- MassTransit se configura **solo para publicar** (`IPublishEndpoint`), no para consumir. No hay `AddConsumer`, `AddEntityFrameworkOutbox`, `QueryDelay` ni `UseBusOutbox` en esta configuración.
- El worker `CustomOutboxProcessorWorker` se ejecuta como `BackgroundService` y gestiona el ciclo de lectura/publicación/confirmación.
- `ConfigureEndpoints()` está presente pero solo se usa para la configuración de endpoints de publicación de MassTransit, no para consumir colas.

[`SoftwareLearningGuide.OutboxProcessor\Options\DatabaseOptions.cs`](SoftwareLearningGuide.OutboxProcessor\Options\DatabaseOptions.cs) | [`SoftwareLearningGuide.OutboxProcessor\Options\MessageBrokerOptions.cs`](SoftwareLearningGuide.OutboxProcessor\Options\MessageBrokerOptions.cs)

> **¿Por qué la API y el OutboxProcessor tienen configuraciones de MassTransit diferentes?** Porque la API solo **ESCRIBE** en el outbox (via `IOutboxWriter`), mientras que el OutboxProcessor solo **LEE** del outbox y **PUBLICA** a RabbitMQ. Son roles opuestos: uno es productor de mensajes, el otro es el intermediario que los entrega. La API no necesita consumers ni endpoints de consumo; el OutboxProcessor no necesita `EntityFrameworkOutbox` porque gestiona la lectura del outbox con Dapper de forma personalizada.

---

## Consumers

### OrderCreatedConsumer

A diferencia de otros consumidores que se ejecutan dentro del OutboxProcessor, `OrderCreatedConsumer` pertenece al proyecto `SoftwareLearningGuide.Consumer`, que es un servicio de consumo independiente. Escucha la cola `order-created` en RabbitMQ y procesa el evento `OrderCreatedEvent` (no `OrderCreatedIntegrationEvent` como se nombraba anteriormente).

[`SoftwareLearningGuide.Consumer\Consumers\OrderCreatedConsumer.cs`](SoftwareLearningGuide.Consumer\Consumers\OrderCreatedConsumer.cs)

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

[`SoftwareLearningGuide.Consumer\Consumers\OrderCancelledConsumer.cs`](SoftwareLearningGuide.Consumer\Consumers\OrderCancelledConsumer.cs)

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

[`SoftwareLearningGuide.Consumer\Consumers\ProductStockLowConsumer.cs`](SoftwareLearningGuide.Consumer\Consumers\ProductStockLowConsumer.cs)

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

### ProductCreatedConsumer (nuevo)

Este consumer fue agregado recientemente y no aparecía en versiones anteriores del README. Procesa el evento `ProductCreatedEvent` cuando se crea un nuevo producto.

[`SoftwareLearningGuide.Consumer\Consumers\ProductCreatedConsumer.cs`](SoftwareLearningGuide.Consumer\Consumers\ProductCreatedConsumer.cs)

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

> **Los consumers son el punto donde la lógica de negocio externa se ejecuta.** Cada consumer es responsable de un tipo de evento: cuando llega un `OrderCreatedEvent`, se envía un email; cuando llega un `ProductStockLowEvent`, se crea una orden de compra. Los consumers no saben quién creó el evento; solo procesan lo que reciben. Esto permite que el sistema sea extensible: puedes agregar un nuevo consumer (por ejemplo, `OrderCreatedAnalyticsConsumer`) sin modificar nada en la API o en el dominio.

### Consumers Registrados

Los consumers están registrados en `SoftwareLearningGuide.Consumer\Program.cs` (no en el OutboxProcessor):

[`SoftwareLearningGuide.Consumer\Program.cs`](SoftwareLearningGuide.Consumer\Program.cs)

| Consumer | Evento | Cola RabbitMQ | Uso |
|----------|--------|---------------|-----|
| `OrderCreatedConsumer` | `OrderCreatedEvent` | `order-created` | Notificaciones, email, inventario |
| `OrderCancelledConsumer` | `OrderCancelledEvent` | `order-cancelled` | Reversar inventario, notificar |
| `ProductCreatedConsumer` | `ProductCreatedEvent` | `product-created` | Notificaciones de nuevo producto |
| `ProductStockLowConsumer` | `ProductStockLowEvent` | `product-stock-low` | Alertas de reaprovisionamiento |

---

## Flujo Completo Paso a Paso

### Escenario: Crear una Order

```
1. POST /api/v1/order
   └── CreateOrderCommandHandler.Handle()
       ├── new Order(orderId, customerId, address)
       │   └──► AddDomainEvent(OrderCreatedDomainEvent)  [en memoria]
       ├── order.AddProduct(product, quantity)
       ├── order.Confirm()
       ├── _orderRepository.AddAsync(order)
       └── _unitOfWork.SaveChangesAsync()
           │
           ▼
2. UnitOfWork.DispatchDomainEventsAsync()
   ├── Busca entidades ProduceEvents con eventos pendientes
   ├── IMediator.Publish(domainEvent)
   │   └── OrderCreatedNotificationHandler.Handle()
   │       └── IOutboxWriter.WriteAsync(OrderCreatedEvent)  ← Guarda en DomainOutboxMessages
   └── _context.SaveChangesAsync()
       │
       ▼
3. SQL Transaction (atómica)
   ├── INSERT INTO Orders (...)
   ├── INSERT INTO OrderLines (...)
   ├── INSERT INTO DomainOutboxMessages (Type='OrderCreatedEvent', Content=...)
   └── COMMIT  ← ATOMICO
       │
       ▼
4. OutboxProcessor (cada 5 segundos)
   ├── Lee DomainOutboxMessages via Dapper WHERE ProcessedOnUtc IS NULL
   ├── Deserializa el Content JSON al tipo conocido (OrderCreatedEvent)
   ├── Publica OrderCreatedEvent a RabbitMQ via IPublishEndpoint
   ├── UPDATE DomainOutboxMessages SET ProcessedOnUtc = GETUTCDATE()
   └── Elimina el mensaje marcándolo como procesado
       │
       ▼
5. OrderCreatedConsumer (en Consumer service)
   └── Consume el mensaje de RabbitMQ de la cola order-created
       └── Ejecuta lógica de negocio externa
```

[`SoftwareLearningGuide.Infraestructure\Repositories\UnitOfWork.cs`](SoftwareLearningGuide.Infraestructure\Repositories\UnitOfWork.cs) | [`SoftwareLearningGuide.OutboxProcessor\Workers\CustomOutboxProcessorWorker.cs`](SoftwareLearningGuide.OutboxProcessor\Workers\CustomOutboxProcessorWorker.cs) | [`SoftwareLearningGuide.Application.Command\NotificationHandlers\OrderCreatedNotificationHandler.cs`](SoftwareLearningGuide.Application.Command\NotificationHandlers\OrderCreatedNotificationHandler.cs)

> **¿Por qué tantos pasos?** Porque cada paso resuelve un problema específico: el dominio valida reglas, el `UnitOfWork` garantiza atomicidad, el Outbox previene pérdida de mensajes, y el Consumer ejecuta lógica externa. Si eliminamos cualquier paso, perdemos una garantía. Por ejemplo, si quitamos el Outbox, volveríamos al problema de "dual escritura". Si quitamos el UnitOfWork, no tendríamos atomicidad entre la entidad y el outbox.

### Tabla DomainOutboxMessages

| Id | Type | Content | CreatedOnUtc | ProcessedOnUtc | Error |
|----|------|---------|--------------|----------------|-------|
| 1 | `OrderCreatedEvent` | `{OrderId: "...", CustomerId: "...", ...}` | 2026-01-15 10:30:00 | NULL | NULL |

[`SoftwareLearningGuide.Infraestructure.Data\Entities\OutboxMessageEntity.cs`](SoftwareLearningGuide.Infraestructure.Data\Entities\OutboxMessageEntity.cs) | [`SoftwareLearningGuide.Infraestructure.Data\Configurations\OutboxMessageConfiguration.cs`](SoftwareLearningGuide.Infraestructure.Data\Configurations\OutboxMessageConfiguration.cs)

**Nota sobre la configuración de la tabla:**

- La tabla se mapea como `DomainOutboxMessages` (no `OutboxMessage`) via `ToTable("DomainOutboxMessages")`.
- El campo `Type` almacena el nombre del tipo C# del evento de integración (e.g., `OrderCreatedEvent`).
- El campo `Content` almacena el JSON serializado del evento.
- El índice compuesto `(ProcessedOnUtc, CreatedOnUtc)` optimiza las consultas del OutboxProcessor para seleccionar mensajes pendientes en orden cronológico.
- La migración original de MassTransit (`OutboxMessage`, `InboxState`, `OutboxState`) fue reemplazada por la tabla personalizada `DomainOutboxMessages` en la migración `AddDomainOutboxMessagesTable` (`SoftwareLearningGuide.Infraestructure.Data\Migrations\20260726173737_AddDomainOutboxMessagesTable.cs`).

---

## Docker Compose

El stack incluye RabbitMQ y el OutboxProcessor:

### Servicios Agregados

| Servicio | Puerto | Descripcion |
|----------|--------|-------------|
| **RabbitMQ** | 5672 (AMQP), 15672 (Management) | Message broker |
| **OutboxProcessor** | — | Worker service que procesa el Outbox |
| **Consumer** | — | Servicio de consumo de eventos |

### Iniciar el Stack

```bash
# Levantar todo (API + SQL + RabbitMQ + OutboxProcessor + Consumer)
docker-compose up -d

# Ver logs del OutboxProcessor
docker-compose logs -f outbox-processor

# Ver logs del Consumer
docker-compose logs -f consumer

# Ver logs de RabbitMQ
docker-compose logs -f rabbitmq

# Acceder a la Management UI de RabbitMQ
# http://localhost:15672 (guest/guest)
```

### Variables de Entorno del OutboxProcessor

Los valores reales se leen de `appsettings.json` y `appsettings.Development.json`:

[`SoftwareLearningGuide.OutboxProcessor\appsettings.Development.json`](SoftwareLearningGuide.OutboxProcessor\appsettings.Development.json)

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

**Nota:** El OutboxProcessor ahora usa opciones de configuración (`DatabaseOptions`, `MessageBrokerOptions`, `OpenTelemetryOptions`) en lugar de variables de entorno hardcodeadas. Esto permite configurar los valores por entorno sin modificar el código.

[`SoftwareLearningGuide.OutboxProcessor\Options\DatabaseOptions.cs`](SoftwareLearningGuide.OutboxProcessor\Options\DatabaseOptions.cs) | [`SoftwareLearningGuide.OutboxProcessor\Options\MessageBrokerOptions.cs`](SoftwareLearningGuide.OutboxProcessor\Options\MessageBrokerOptions.cs)

---

## Por que esta Arquitectura es la Mejor Solucion

### 1. Atomicidad Garantizada (ACID)

El mensaje y la entidad se guardan en la **misma transacción SQL**. Si la transacción falla, no se genera ningún mensaje en `DomainOutboxMessages`. No hay estado inconsistente.

> **¿Qué pasa si la transacción SQL falla después del INSERT pero antes del COMMIT?** No se guarda nada: ni la entidad ni el mensaje. La app puede reintentar la operación completa sin riesgo de duplicados. Sin el Outbox, el mensaje podría haberse enviado a RabbitMQ mientras la entidad no se guardó, creando una inconsistencia silenciosa.

### 2. No Pierde Mensajes

Si RabbitMQ está caído, los mensajes permanecen en la tabla `DomainOutboxMessages`. Cuando el broker se recupera, el OutboxProcessor los procesa automáticamente.

> **¿Qué pasa si RabbitMQ se cae mientras el OutboxProcessor está procesando?** Los mensajes que ya se publicaron se pierden (a menos que el consumer falle y RabbitMQ los reencolé). Los mensajes que aún están en `DomainOutboxMessages` se quedan pendientes y se procesan cuando el broker se recupere. La clave es que el OutboxProcessor reintenta automáticamente: no necesitas intervención manual.

### 3. Desacoplamiento Total

La API no sabe quién consume los eventos. Puedes agregar consumers en otros microservicios sin modificar la API. El dominio solo genera eventos; no le importa quién los consume.

> **Ejemplo concreto:** Imagina que mañana necesitas agregar un consumer que envíe un SMS cuando se crea una orden. Solo necesitas crear `OrderCreatedSmsConsumer` y registrarlo en el Consumer service. La API no se modifica, el dominio no se modifica, y el consumer existente sigue funcionando. Es como agregar un nuevo destinatario en el buzón: la carta ya está escrita, solo cambia quién la recibe.

### 4. Escalabilidad

Puedes agregar más `NotificationHandlers` o `Consumers` sin modificar el dominio. El OutboxProcessor puede escalar horizontalmente (con bloqueo optimista en la tabla via el campo `ProcessedOnUtc`).

> **Ejemplo concreto:** Si tu sistema procesa 10,000 órdenes por minuto, puedes ejecutar 3 instancias del OutboxProcessor. Cada una consulta `DomainOutboxMessages` con un filtro optimista (`ProcessedOnUtc IS NULL`) para que no procesen el mismo mensaje dos veces. La tabla `DomainOutboxMessages` actúa como una cola persistente que sobrevive reinicios y caídas.

### 5. Observabilidad

Cada mensaje en `DomainOutboxMessages` tiene un `Id` único que permite correlacionar el flujo completo desde la creación hasta el procesamiento por el consumer final. Los logs estructurados facilitan el debugging.

> **Ejemplo concreto:** Cuando un cliente reporta "no recibí mi email de confirmación", puedes buscar el `Id` del mensaje en los logs de la API (donde se creó el evento en `DomainOutboxMessages`), en los logs del OutboxProcessor (donde se publicó), y en los logs del consumer (donde se procesó). Si el evento no aparece en la tabla, el problema está en la transacción. Si aparece pero no en el consumer, el problema está en RabbitMQ.

### 6. Recuperación Automática

Si el OutboxProcessor se cae, al reiniciar continúa procesando mensajes pendientes. No se pierde nada.

> **Ejemplo concreto:** Si el OutboxProcessor se cae a las 2 AM y se reinicia a las 6 AM, los mensajes que llegaron entre las 2 y las 6 siguen en `DomainOutboxMessages` con `ProcessedOnUtc IS NULL`. Al reiniciar, el processor los procesa en orden cronológico (ordenado por `CreatedOnUtc`). No hay pérdida de datos, no hay mensajes duplicados (gracias al `ProcessedOnUtc` marking), y no necesitas intervención manual. Es como si el cartero se enfermara un día y al día siguiente entregara todos los correos acumulados.

---

## Recursos Recomendados

- [Transactional Outbox Pattern - Microservices.io](https://microservices.io/patterns/data/transactional-outbox.html)
- [MassTransit Outbox Documentation](https://masstransit.io/documentation/configuration/persistence/entity-framework)
- [Outbox Pattern - Chris Richardson](https://microservices.io/patterns/data/transactional-outbox.html)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)

---

**Ultima actualizacion:** 2026
**Proyecto:** SoftwareLearningGuide.OutboxProcessor
