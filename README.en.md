<h1 align="center">Software Learning Guide</h1>

<p align="center">
  <em>Educational project for learning and practicing modern development concepts in <strong>.NET 10</strong></em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/ASP.NET_Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core">
  <img src="https://img.shields.io/badge/CQRS-FF6B6B?style=for-the-badge" alt="CQRS">
  <img src="https://img.shields.io/badge/DDD-4ECDC4?style=for-the-badge" alt="DDD">
  <img src="https://img.shields.io/badge/Clean_Architecture-45B7D1?style=for-the-badge" alt="Clean Architecture">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/MediatR-8B5CF6?style=for-the-badge&logo=mediatr&logoColor=white" alt="MediatR">
  <img src="https://img.shields.io/badge/MassTransit-FF4655?style=for-the-badge" alt="MassTransit">
  <img src="https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white" alt="RabbitMQ">
  <img src="https://img.shields.io/badge/EF_Core-68A063?style=for-the-badge&logo=entityframework&logoColor=white" alt="EF Core">
  <img src="https://img.shields.io/badge/Dapper-DDDDEE?style=for-the-badge&logo=dapper&logoColor=black" alt="Dapper">
  <img src="https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/OpenTelemetry-60E6CC?style=for-the-badge&logo=opentelemetry&logoColor=black" alt="OpenTelemetry">
  <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker">
  <img src="https://img.shields.io/badge/Aspire_Dashboard-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="Aspire Dashboard">
  <img src="https://img.shields.io/badge/Flagsmith-3B82F6?style=for-the-badge" alt="Flagsmith">
</p>

<p align="center">
  <a href="README.md">
    <img src="https://img.shields.io/badge/Español-ES-blue?style=for-the-badge&labelColor=FFD700&color=0066CC" alt="Español">
  </a>
  <a href="https://acorangonzalezmoray.github.io/SoftwareLearningGuide/#/README.en">
    <img src="https://img.shields.io/badge/Documentation-Web-4ECDC4?style=for-the-badge&logo=gitbook&logoColor=white" alt="Web Documentation">
  </a>
</p>

> **📖 For a better reading experience, check the [interactive web documentation](https://acorangonzalezmoray.github.io/SoftwareLearningGuide/#/README.en) with Mermaid diagrams, search, and improved navigation.**

---

## What You'll Learn

| Area | Key Concepts | Badge |
|------|--------------|-------|
| **Architecture** | Clean Architecture, Vertical Slicing, SOLID, Dependency Inversion | `Architecture` |
| **Domain (DDD)** | Value Objects, Entities, Aggregates, Domain Events, DomainErrors | `DDD` |
| **Result\<T\> Pattern** | Functional error handling without exceptions, composition | `Result Pattern` |
| **CQRS** | Command/Query separation with MediatR, Handlers, Pipeline Behaviors | `CQRS` |
| **Unit of Work** | Transactionality, atomicity, event dispatching | `UoW` |
| **Events** | Domain Events, Integration Events, Notification Handlers | `Events` |
| **Transactional Outbox** | Outbox Pattern with MassTransit + RabbitMQ, Workers | `Outbox` |
| **Resilience** | Retry, Circuit Breaker, Timeout in MassTransit | `Resilience` |
| **Persistence** | EF Core (write), Dapper (read), Value Objects in DB | `Data` |
| **REST APIs** | Controllers, versioning, OpenAPI documentation, streaming | `API` |
| **Observability** | Distributed tracing, metrics, structured logging, scopes | `Observability` |
| **Feature Flags** | Feature Toggles with Microsoft.FeatureManagement and Flagsmith | `Feature Mgmt` |
| **Infrastructure** | Docker, Aspire Dashboard, RabbitMQ, SQL Server | `Infra` |

---

## Documentation

### To Learn About...

| Topic | Location | Description |
|-------|----------|-------------|
| **Domain-Driven Design (DDD)** | [`README.DDD.en.md`](docs/SoftwareLearningGuide.Core.Business/README.DDD.en.md) | Value Objects, Entities, Aggregates, practical examples |
| **Domain Events** | [`README.EventSourcing.en.md`](docs/SoftwareLearningGuide.Core.Business/README.EventSourcing.en.md) | Domain events, MediatR, Notification Handlers, Integration Events |
| **Transactional Outbox** | [`README.OutboxPattern.en.md`](docs/SoftwareLearningGuide.OutboxProcessor/README.OutboxPattern.en.md) | Outbox Pattern with MassTransit + RabbitMQ, atomicity, Workers |
| **Resilience** | [`README.Resilience.en.md`](docs/SoftwareLearningGuide.OutboxProcessor/README.Resilience.en.md) | Retry, Circuit Breaker, Timeout in the MassTransit pipeline |
| **Result\<T\> Pattern** | [`README.ResultPattern.en.md`](docs/SoftwareLearningGuide.Core.Business/README.ResultPattern.en.md) | Functional error handling, composition, best practices |
| **DomainErrors** | [`README.Errors.en.md`](docs/SoftwareLearningGuide.Core.Business/README.Errors.en.md) | Centralized error catalog with entity context and ID |
| **CQRS with MediatR** | [`README.CQRS.en.md`](docs/SoftwareLearningGuide.Application/README.CQRS.en.md) | Commands, Queries, Handlers, Mediator pattern |
| **Unit of Work** | [`README.UnitOfWork.en.md`](docs/SoftwareLearningGuide.Infraestructure/README.UnitOfWork.en.md) | Transactionality, atomicity, Unit of Work pattern |
| **Entity Framework Core** | [`README.EntityFramework.en.md`](docs/SoftwareLearningGuide.Infraestructure.Data/README.EntityFramework.en.md) | DbContext, configurations, Value Objects, Owned Types |
| **Dapper** | [`README.Dapper.en.md`](docs/SoftwareLearningGuide.Application.Query/README.Dapper.en.md) | Direct SQL, CommandDefinition, result mapping |
| **Middleware** | [`README.Middleware.en.md`](docs/SoftwareLearningGuide.Api/README.Middleware.en.md) | Global Exception Handling, logging scopes, HTTP pipeline |
| **Building APIs** | [`README.Api.en.md`](docs/SoftwareLearningGuide.Api/README.Api.en.md) | REST API concepts, versioning, OpenAPI |
| **Observability** | [`README.Observability.en.md`](docs/SoftwareLearningGuide.Api/README.Observability.en.md) | The 3 pillars: traces, metrics, logs, scopes, exporters |
| **Feature Management** | [`README.FeatureManagement.en.md`](docs/SoftwareLearningGuide.Api/README.FeatureManagement.en.md) | Feature Toggles, Feature Flags, Microsoft.FeatureManagement, Flagsmith |
| **Vertical Slicing** | [`README.VerticalSlicing.en.md`](docs/SoftwareLearningGuide.Application/README.VerticalSlicing.en.md) | Organization by features, autonomous slices, Open/Closed in practice |
| **Clean Architecture** | [`README.CleanArchitecture.en.md`](docs/SoftwareLearningGuide.Api/README.CleanArchitecture.en.md) | Layers, dependency rule, Ports & Adapters, technology independence |

#### Design Patterns

| Pattern | Location | Description |
|---------|----------|-------------|
| **SOLID** | [`README.Pattern.SOLID.en.md`](docs/SoftwareLearningGuide.Application/README.Pattern.SOLID.en.md) | The 5 SOLID principles with concrete project examples |
| **Factory** | [`README.Pattern.Factory.en.md`](docs/SoftwareLearningGuide.Application/README.Pattern.Factory.en.md) | Static Factory Method in Value Objects and Aggregates |
| **Builder** | [`README.Pattern.Builder.en.md`](docs/SoftwareLearningGuide.Application/README.Pattern.Builder.en.md) | Fluent Builder for tests, complex Commands, and records with `with` |
| **Repository** | [`README.Pattern.Repository.en.md`](docs/SoftwareLearningGuide.Application/README.Pattern.Repository.en.md) | Data access abstraction, write vs read repositories |
| **Mediator** | [`README.Pattern.Mediator.en.md`](docs/SoftwareLearningGuide.Application/README.Pattern.Mediator.en.md) | MediatR: Send vs Publish, Pipeline Behaviors, automatic discovery |

#### Testing References

| Concept | Location | Description |
|---------|----------|-------------|
| **TDD** | [`README.TDD.en.md`](docs/tests/README.TDD.en.md) | Red-Green-Refactor cycle, testing best practices |
| **TestContainers** | [`README.TestContainer.en.md`](docs/tests/README.TestContainer.en.md) | Disposable Docker containers for integration tests |
| **Respawn** | [`README.Respawn.en.md`](docs/tests/README.Respawn.en.md) | Automatic database reset between tests |
| **Response Fixture** | [`README.ResponseFixture.en.md`](docs/tests/README.ResponseFixture.en.md) | JSON files with expected responses for assertions |

---

## Quick Start

### Prerequisites

- **.NET 10 SDK** or higher
- **Docker Desktop** (for observability services and SQL Server)
- **Visual Studio 2026** (recommended) or VS Code

### Run the API

The project contains **three applications** that run independently:

#### 1. Main API (`SoftwareLearningGuide.Api`)

```bash
# Run the REST API
dotnet run --project SoftwareLearningGuide.Api

# Or from the project folder
cd SoftwareLearningGuide.Api
dotnet run

# Or from Visual Studio: F5
```

> **Ports:** `http://localhost:5089` (HTTP) | `https://localhost:7033` (HTTPS)

#### 2. Outbox Processor (`SoftwareLearningGuide.OutboxProcessor`)

Worker Service that reads the OutboxMessage table and publishes events to RabbitMQ. It contains no consumption logic - it only publishes.

```bash
# Run the Outbox Processor
dotnet run --project SoftwareLearningGuide.OutboxProcessor

# Or from the project folder
cd SoftwareLearningGuide.OutboxProcessor
dotnet run
```

> **Requires:** SQL Server and RabbitMQ running (`docker-compose up -d`)

#### 3. Consumer (`SoftwareLearningGuide.Consumer`)

Worker Service that listens to RabbitMQ queues and processes events published by the OutboxProcessor.

```bash
# Run the Consumer
dotnet run --project SoftwareLearningGuide.Consumer

# Or from the project folder
cd SoftwareLearningGuide.Consumer
dotnet run
```

> **Requires:** RabbitMQ running (`docker-compose up -d`)

#### Available Endpoints

**Main API (`localhost:5089`):**

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1.0/order` | Get all orders |
| GET | `/api/v1.0/order/{id}` | Get order by GUID |
| POST | `/api/v1.0/order` | Create an order |
| GET | `/api/v1.0/product` | Get all products |
| GET | `/api/v1.0/product/{id}` | Get product by GUID |
| POST | `/api/v1.0/product` | Create a product |
| GET | `/api/v1.0/customer` | Get all customers |
| GET | `/api/v1.0/customer/{id}` | Get customer by GUID |
| POST | `/api/v1.0/customer` | Create a customer |
| GET | `/api/v1.0/weatherforecast` | Weather forecast (v1) |
| GET | `/api/v2.0/weatherforecast` | Weather forecast (v2, with metrics) |
| GET | `/api/v2.0/apirest` | List items (with filter and pagination) |
| GET | `/api/v2.0/apirest/{id}` | Get item by ID |
| POST | `/api/v2.0/apirest` | Create item |
| PUT | `/api/v2.0/apirest/{id}` | Replace item (full update) |
| PATCH | `/api/v2.0/apirest/{id}` | Update item (partial) |
| DELETE | `/api/v2.0/apirest/{id}` | Delete item |
| POST | `/api/v2.0/apirest/upload` | Upload file (multipart, max 10MB) |
| GET | `/api/v2.0/apirest/stream` | Stream items (IAsyncEnumerable) |
| GET | `/api/v2.0/apirest/headers` | Read header X-Request-ID |
| GET | `/api/v2.0/apirest/forbidden` | Example 403 response |
| GET | `/api/v2.0/apirest/unauthorized` | Example 401 response |
| GET | `/api/v2.0/apirest/problem` | Example 500 response (ProblemDetails) |

**Documentation:**
- **Scalar:** `https://localhost:7033/scalar/v1`
- **Swagger UI:** `https://localhost:7033/swagger`
- **OpenAPI Spec:** `https://localhost:7033/openapi/v1.json`

> **Note:** Order, Product, and Customer controllers are protected by Feature Flags. In Development mode all are enabled.

---

## Docker Compose

The current stack includes:

- **Aspire Dashboard** - .NET observability control panel
- **SQL Server 2022** - Application database
- **Flagsmith** - Feature flags management service
- **PostgreSQL** - Flagsmith database
- **RabbitMQ** - Message broker for service integration

### Available Services

| Service | URL | Port | Description |
|---------|-----|------|-------------|
| **Aspire Dashboard** | http://localhost:18888 | 18888 | Traces, metrics, and logs panel |
| **OTEL Collector (gRPC)** | localhost:4317 | 4317 | Receives telemetry from the app |
| **SQL Server** | localhost,1433 | 1433 | SoftwareLearningGuide database |
| **Flagsmith** | http://localhost:8000 | 8000 | Feature flags panel |
| **PostgreSQL** | localhost:5432 | 5432 | Flagsmith database |
| **RabbitMQ** | localhost:5672 | 5672 | Message broker (AMQP) |
| **RabbitMQ Management** | http://localhost:15672 | 15672 | RabbitMQ administration UI (guest/guest) |

### Useful Commands

```bash
# Start the stack
docker-compose up -d

# View logs
docker-compose logs -f aspire-dashboard

# Stop the stack
docker-compose down

# View service status
docker-compose ps

# Connect to SQL Server (from the app)
# ConnectionString: Server=localhost,1433;Database=SoftwareLearningGuide;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;
```

---

## Learning Objectives

- Understand REST architecture with versioning
- Implement Domain-Driven Design with Value Objects and Aggregates
- Implement Domain Events for total decoupling in the domain
- Use the Transactional Outbox pattern for database and messaging atomicity
- Use the Result\<T\> pattern for functional error handling
- Centralize domain errors with DomainErrors for descriptive and consistent messages
- Implement CQRS with MediatR to separate reads and writes
- Apply the Unit of Work pattern for transactionality and atomicity in writes
- Use Dapper for direct read queries against the database
- Use Entity Framework Core for commands with tracking and relationships
- Implement modern observability with OpenTelemetry (traces, metrics, logs)
- Use structured logging and scopes for request correlation
- Expose custom business metrics
- Document APIs with OpenAPI/Swagger
- Containerize .NET applications with Docker
- Monitor applications in production
- Manage feature toggles and flags with Microsoft.FeatureManagement and Flagsmith

---

## Folder Structure

```
SoftwareLearningGuide/
├── SoftwareLearningGuide.Core.Business/            # Domain Layer (DDD)
│   ├── Exceptions/                                 # Domain exceptions + Result<T>
│   ├── Errors/                                     # DomainErrors: centralized error catalog
│   ├── ValueObjects/                               # Money, Email, Address, OrderId, etc.
│   ├── Entities/                                   # Product, Customer, OrderLine
│   └── Aggregates/                                 # Order (aggregate root)
│
├── SoftwareLearningGuide.Application/              # Application Layer
│   ├── SoftwareLearningGuide.Application.Query/    # Queries (Dapper, read)
│   │   └── GetOrder/                               # GetOrderQuery + Handler
│   ├── SoftwareLearningGuide.Application.Command/  # Commands (EF Core, write)
│   │   ├── CreateOrder/                            # CreateOrderCommand + Handler
│   │   └── Ports/                                  # IOrderWriteRepository, IUnitOfWork
│   ├── README.CQRS.md                              # CQRS with MediatR guide
│   ├── README.VerticalSlicing.md                   # Organization by features (slices)
│   ├── README.Pattern.SOLID.md                     # SOLID principles with project examples
│   ├── README.Pattern.Factory.md                   # Factory pattern in Value Objects and Aggregates
│   ├── README.Pattern.Builder.md                   # Builder pattern for tests and complex Commands
│   ├── README.Pattern.Repository.md                # Repository pattern: ports and adapters
│   └── README.Pattern.Mediator.md                  # Mediator pattern with MediatR
│
├── SoftwareLearningGuide.Infraestructure/          # Infrastructure Layer
│   ├── SoftwareLearningGuide.Infraestructure.Data/ # EF Core, DbContext, Configurations
│   │   ├── Context/                                # ApplicationDbContext
│   │   ├── Configurations/                         # OrderConfiguration, etc.
│   │   └── Repositories/                           # OrderWriteRepository, UnitOfWork
│   ├── Ports/                                      # IOrderWriteRepository
│   └── Dependencies.cs                             # DI registration
│
├── SoftwareLearningGuide.Api/                      # ASP.NET Core REST API
│   ├── Controllers/                                # OrderController, WeatherForecast, etc.
│   ├── Extensions/                                 # OpenTelemetry, ApiVersioning, Options, ConfigurationBuilder
│   ├── FeatureToggles/                             # Feature Management: constants, custom provider
│   ├── Middlewares/                                 # GlobalExceptionMiddleware
│   ├── Metrics/                                    # OrderMetrics (business metrics)
│   ├── Startup/                                    # DI: Commands, Queries, Repos, Metrics
│   ├── Options/                                    # OpenTelemetryOptions, DatabaseOptions, FeatureManagementApiConfigurationOptions
│   ├── appsettings.json                            # Base configuration (empty values)
│   ├── appsettings.Development.json                # Development configuration (real values)
│   ├── README.CleanArchitecture.md                 # Layers, dependency rule, Ports & Adapters
│   └── Program.cs                                  # Entry point
│
├── SoftwareLearningGuide.OutboxProcessor/          # Worker Service: Outbox → RabbitMQ
│   ├── Options/                                    # DatabaseOptions, MessageBrokerOptions, OpenTelemetryOptions
│   ├── Extensions/                                 # Option configuration, OpenTelemetry
│   ├── Program.cs                                  # Entry point with MassTransit (publish only)
│   ├── appsettings.json                            # ConnectionStrings + RabbitMQ config
│   └── README.OutboxPattern.md                     # Outbox Pattern guide
│
├── SoftwareLearningGuide.Consumer/                 # Worker Service: RabbitMQ → Business Logic
│   ├── Consumers/                                  # OrderCreatedConsumer, ProductCreatedConsumer, etc.
│   ├── Events/Input/                               # Integration Events (input DTOs)
│   ├── Metrics/                                    # ConsumerMetrics (consumption metrics)
│   ├── Options/                                    # MessageBrokerOptions, OpenTelemetryOptions
│   ├── Extensions/                                 # Option configuration, OpenTelemetry
│   ├── Program.cs                                  # Entry point with MassTransit (consume only)
│   └── appsettings.json                            # RabbitMQ config + OpenTelemetry
│
├── docker-compose.yml                              # Stack: Aspire + SQL Server + Flagsmith + PostgreSQL + RabbitMQ
└── README.md                                       # This file
```

---

## Complete Flow: API → Outbox → Consumer (Create Product Case)

This section explains how the 3 system artifacts interact when a client creates a product. It's the practical example that integrates all project patterns.

### The 3 Artifacts

```
┌─────────────────────────────────────────────────────────────────┐
│  1. API (SoftwareLearningGuide.Api)                             │
│     Receives HTTP, validates, persists, generates events        │
├─────────────────────────────────────────────────────────────────┤
│  2. OutboxProcessor (SoftwareLearningGuide.OutboxProcessor)     │
│     Reads OutboxMessage table, publishes to RabbitMQ            │
├─────────────────────────────────────────────────────────────────┤
│  3. Consumer (SoftwareLearningGuide.Consumer)                   │
│     Listens to RabbitMQ, executes external business logic       │
└─────────────────────────────────────────────────────────────────┘
```

### Step 1: Client sends POST /api/v1/product

The request arrives at `ProductController`, which dispatches a `CreateProductCommand` via MediatR:

```
POST /api/v1/product
Body: { "name": "Laptop", "description": "Gaming laptop", "price": 1200.00, "currency": "USD", "stock": 10 }

    │
    ▼
ProductController.Create()
    │  [FeatureGate: FT_ENABLE_PRODUCT_CONTROLLER + FT_ENABLE_PRODUCT_CREATION]
    │
    ▼
IMediator.Send(CreateProductCommand)
    │
    ▼
CreateProductCommandHandler.Handle()
```

### Step 2: The Command Handler creates the domain

The handler creates the `Product` entity which **inherits from `ProduceEvents`**, validating business rules and triggering a domain event:

```csharp
// Product.cs inherits from ProduceEvents
var product = new Product(productId, "Laptop", "Gaming laptop",
    new Money(1200.00m, "USD"), 10);
// Internally executes:
//   AddDomainEvent(new ProductCreatedDomainEvent { ... })
// The event remains in memory within the _domainEvents list
```

### Step 3: UnitOfWork persists everything atomically

The handler calls `_unitOfWork.SaveChangesAsync()` which does two things:

```
unitOfWork.SaveChangesAsync()
    │
    ├── 1. DispatchDomainEventsAsync()
    │      │
    │      ├── Finds ProduceEvents entities with pending events
    │      │   └── Finds Product with ProductCreatedDomainEvent
    │      │
    │      ├── IMediator.Publish(ProductCreatedDomainEvent)
    │      │   └── ProductCreatedNotificationHandler.Handle()
    │      │       │
    │      │       └── Creates ProductCreatedIntegrationEvent
    │      │           └── IPublishEndpoint.Publish(integrationEvent)
    │      │               └── MassTransit writes to OutboxMessage
    │      │                   (IN THE SAME SQL TRANSACTION)
    │      │
    │      └── ClearDomainEvents() ← Clears to prevent reprocessing
    │
    └── 2. _context.SaveChangesAsync()
           │
           ├── INSERT INTO Products (...) VALUES (...)
           └── INSERT INTO OutboxMessage (...) VALUES (...)
           └── COMMIT ← ATOMIC: product + message together
```

**Key:** The product and outbox message are saved in the **same SQL transaction**. If something fails, the product is not saved and no message is generated.

### Step 4: OutboxProcessor publishes to RabbitMQ

The `OutboxProcessor` is a Worker Service that checks the `OutboxMessage` table every 5 seconds:

```
OutboxProcessor (every 5 seconds)
    │
    ├── SELECT * FROM OutboxMessage WHERE Sent IS NULL
    │   └── Finds the record with ProductCreatedIntegrationEvent
    │
    ├── MassTransit reads the message body
    │
    ├── Publishes to RabbitMQ
    │   └── Exchange: ProductCreatedIntegrationEvent
    │       └── Queue: product-created
    │
    └── UPDATE OutboxMessage SET Sent = GETUTCDATE()
        (or DELETE, depending on configuration)
```

**Note:** If RabbitMQ is down, the OutboxProcessor automatically retries when the broker recovers. No messages are lost.

### Step 5: Consumer processes the event

The `ProductCreatedConsumer` in the Consumer service listens to the `product-created` queue:

```
Consumer (listens to RabbitMQ)
    │
    └── ProductCreatedConsumer.Handle()
        │
        ├── Receives ProductCreatedIntegrationEvent:
        │   { ProductId: "...", Name: "Laptop", Price: 1200, Currency: "USD" }
        │
        ├── Confirmation log
        │
        └── External business logic (example):
            ├── Send welcome email to catalog
            ├── Update product cache
            └── Notify inventory system
```

### Complete Visual Diagram

```
   Client                    API                    SQL Server            RabbitMQ              Consumer
      │                         │                         │                    │                     │
      │  POST /api/v1/product   │                         │                    │                     │
      │─────────────────────────│                         │                    │                     │
      │                         │                         │                    │                     │
      │                         │    ProductController    │                    │                     │
      │                         │    FeatureGate check    │                    │                     │
      │                         │                         │                    │                     │
      │                         │    CreateProductHandler │                    │                     │
      │                         │    new Product(...)     │                    │                     │
      │                         │    AddDomainEvent(...)  │                    │                     │
      │                         │                         │                    │                     │
      │                         │UnitOfWork.SaveChangesAsy│                    │                     │
      │                         │                         │                    │                     │
      │                         │DispatchDomainEventsAsync│                    │                     │
      │                         │                         │                    │                     │
      │                         │    IMediator.Publish()  │                    │                     │
      │                         │                         │                    │                     │
      │                         │    NotificationHandler  │                    │                     │
      │                         │                         │                    │                     │
      │                         │  MassTransit → OutboxMes│                    │                     │
      │                         │                         │                    │                     │
      │                         │  BEGIN TRANSACTION      │                    │                     │
      │                         │  INSERT INTO Products   │                    │                     │
      │                         │  INSERT INTO OutboxMsg  │                    │                     │
      │                         │  COMMIT                 │                    │                     │
      │                         │◄────────────────────────│                    │                     │
      │                         │                         │                    │                     │
      │  201 Created            │                         │                    │                     │
      │◄────────────────────────│                         │                    │                     │
      │                         │                         │                    │                     │
      │                         │OutboxProcessor (every 5s│                    │                     │
      │                         │  SELECT FROM OutboxMsg  │                    │                     │
      │                         │────────────────────────►│                    │                     │
      │                         │                         │                    │                     │
      │                         │  Publishes to RabbitMQ  │                    │                     │
      │                         │────────────────────────────────────────────-►│                     │
      │                         │                         │                    │                     │
      │                         │  UPDATE Sent = NOW()    │                    │                     │
      │                         │────────────────────────►│                    │                     │
      │                         │                         │                    │                     │
      │                         │                         │                    │product-created queue│
      │                         │                         │                    │  Consumes message   │
      │                         │                         │                    │───────────────────-►│
      │                         │                         │                    │                     │
      │                         │                         │                    │                     │ ProductCreatedConsumer
      │                         │                         │                    │                     │ Log + external logic
```

### What happens if something fails?

| Scenario | Result |
|----------|--------|
| Validation fails in Product | Nothing is created, 400 response with error |
| SQL Server down | Nothing is saved, 500 response (GlobalExceptionMiddleware) |
| RabbitMQ down | Product is saved, OutboxMessage remains pending. OutboxProcessor retries |
| Consumer fails | Message stays in RabbitMQ with retries (at-least-once) |

### Related Documentation

To dive deeper into each artifact, check the dedicated READMEs:

- [`README.DDD.en.md`](docs/SoftwareLearningGuide.Core.Business/README.DDD.en.md) - Product entity and its ProduceEvents inheritance
- [`README.EventSourcing.en.md`](docs/SoftwareLearningGuide.Core.Business/README.EventSourcing.en.md) - Domain Events and their lifecycle
- [`README.UnitOfWork.en.md`](docs/SoftwareLearningGuide.Infraestructure/README.UnitOfWork.en.md) - How UnitOfWork dispatches events and persists
- [`README.OutboxPattern.en.md`](docs/SoftwareLearningGuide.OutboxProcessor/README.OutboxPattern.en.md) - Transactional Outbox with MassTransit
- [`README.CQRS.en.md`](docs/SoftwareLearningGuide.Application/README.CQRS.en.md) - Command/Query separation with MediatR

---

## Technology Stack

<p align="center">
  <img src="https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/ASP.NET_Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="ASP.NET Core">
  <img src="https://img.shields.io/badge/MediatR-8B5CF6?style=for-the-badge" alt="MediatR">
  <img src="https://img.shields.io/badge/MassTransit-FF4655?style=for-the-badge" alt="MassTransit">
  <img src="https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white" alt="RabbitMQ">
  <img src="https://img.shields.io/badge/Dapper-DDDDEE?style=for-the-badge&logo=dapper&logoColor=black" alt="Dapper">
  <img src="https://img.shields.io/badge/EF_Core-68A063?style=for-the-badge&logo=entityframework&logoColor=white" alt="EF Core">
  <img src="https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server">
  <img src="https://img.shields.io/badge/Asp.Versioning-512BD4?style=for-the-badge" alt="Asp.Versioning">
  <img src="https://img.shields.io/badge/OpenTelemetry-60E6CC?style=for-the-badge&logo=opentelemetry&logoColor=black" alt="OpenTelemetry">
  <img src="https://img.shields.io/badge/Scalar-FF4655?style=for-the-badge" alt="Scalar">
  <img src="https://img.shields.io/badge/Swagger_UI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black" alt="Swagger UI">
  <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker">
  <img src="https://img.shields.io/badge/Aspire_Dashboard-512BD4?style=for-the-badge" alt="Aspire Dashboard">
  <img src="https://img.shields.io/badge/FeatureManagement-3B82F6?style=for-the-badge" alt="FeatureManagement">
  <img src="https://img.shields.io/badge/Flagsmith-3B82F6?style=for-the-badge" alt="Flagsmith">
</p>

---

## Support and Resources

- [Microsoft .NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [ASP.NET Core API Best Practices](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [MediatR Documentation](https://automapper.org/)
- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [Dapper Documentation](https://dapperlib.github.io/Dapper/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)

---

**Happy learning!**