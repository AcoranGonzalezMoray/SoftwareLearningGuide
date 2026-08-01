<h1 align="center">Software Learning Guide</h1>

<p align="center">
  <em>Proyecto educativo para aprender y practicar conceptos modernos de desarrollo en <strong>.NET 10</strong></em>
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
  <a href="README.en.md">
    <img src="https://img.shields.io/badge/English-EN-blue?style=for-the-badge&labelColor=FFD700&color=0066CC" alt="English">
  </a>
</p>

---

## Que aprenderas

| Area | Conceptos Clave | Badge |
|------|-----------------|-------|
| **Arquitectura** | Clean Architecture, Vertical Slicing, SOLID, Dependency Inversion | `Architecture` |
| **Dominio (DDD)** | Value Objects, Entidades, Agregados, Domain Events, DomainErrors | `DDD` |
| **Patron Result\<T\>** | Manejo funcional de errores sin excepciones, composicion | `Result Pattern` |
| **CQRS** | Separacion Command/Query con MediatR, Handlers, Pipeline Behaviors | `CQRS` |
| **Unit of Work** | Transaccionalidad, atomicidad, despacho de eventos | `UoW` |
| **Eventos** | Domain Events, Integration Events, Notification Handlers | `Events` |
| **Transactional Outbox** | Patron Outbox con MassTransit + RabbitMQ, Workers | `Outbox` |
| **Resiliencia** | Retry, Circuit Breaker, Timeout en MassTransit | `Resilience` |
| **Persistencia** | EF Core (escritura), Dapper (lectura), Value Objects en DB | `Data` |
| **APIs REST** | Controladores, versionado, documentacion OpenAPI, streaming | `API` |
| **Observabilidad** | Trazas distribuidas, metricas, logging estructurado, scopes | `Observability` |
| **Feature Flags** | Feature Toggles con Microsoft.FeatureManagement y Flagsmith | `Feature Mgmt` |
| **Cloud Providers** | AWS, MiniStack (emulador local), SSM Parameter Store, proveedores de configuracion | `Cloud Providers` |
| **Infraestructura** | Docker, Aspire Dashboard, RabbitMQ, SQL Server | `Infra` |

---

## Documentacion

### Para Aprender Sobre...

| Tema | Ubicacion | Descripcion |
|------|-----------|-------------|
| **Domain-Driven Design (DDD)** | [`README.DDD.md`](SoftwareLearningGuide.Core.Business/README.DDD.md) | Value Objects, Entidades, Agregados, ejemplos practicos |
| **Domain Events** | [`README.EventSourcing.md`](SoftwareLearningGuide.Core.Business/README.EventSourcing.md) | Eventos de dominio, MediatR, Notification Handlers, Integration Events |
| **Transactional Outbox** | [`README.OutboxPattern.md`](SoftwareLearningGuide.OutboxProcessor/README.OutboxPattern.md) | Outbox Pattern con MassTransit + RabbitMQ, atomicidad, Workers |
| **Resiliencia** | [`README.Resilience.md`](SoftwareLearningGuide.OutboxProcessor/README.Resilience.md) | Retry, Circuit Breaker, Timeout en el pipeline de MassTransit |
| **Patron Result\<T\>** | [`README.ResultPattern.md`](SoftwareLearningGuide.Core.Business/README.ResultPattern.md) | Manejo de errores funcional, composicion, mejores practicas |
| **DomainErrors** | [`README.Errors.md`](SoftwareLearningGuide.Core.Business/README.Errors.md) | Catalogo centralizado de errores con contexto de entidad e ID |
| **CQRS con MediatR** | [`README.CQRS.md`](SoftwareLearningGuide.Application/README.CQRS.md) | Commands, Queries, Handlers, patron Mediator |
| **Unit of Work** | [`README.UnitOfWork.md`](SoftwareLearningGuide.Infraestructure/README.UnitOfWork.md) | Transaccionalidad, atomicidad, patron Unit of Work |
| **Entity Framework Core** | [`README.EntityFramework.md`](SoftwareLearningGuide.Infraestructure.Data/README.EntityFramework.md) | DbContext, configuraciones, Value Objects, Owned Types |
| **Dapper** | [`README.Dapper.md`](SoftwareLearningGuide.Application.Query/README.Dapper.md) | SQL directo, CommandDefinition, mapeo de resultados |
| **Middleware** | [`README.Middleware.md`](SoftwareLearningGuide.Api/README.Middleware.md) | Global Exception Handling, scopes de logging, pipeline HTTP |
| **Construccion de APIs** | [`README.Api.md`](SoftwareLearningGuide.Api/README.Api.md) | Conceptos de API REST, versionado, OpenAPI |
| **Observability** | [`README.Observability.md`](SoftwareLearningGuide.Api/README.Observability.md) | Los 3 pilares: trazas, metricas, logs, scopes, exportadores |
| **Feature Management** | [`README.FeatureManagement.md`](SoftwareLearningGuide.Api/README.FeatureManagement.md) | Feature Toggles, Feature Flags, Microsoft.FeatureManagement, Flagsmith |
| **Vertical Slicing** | [`README.VerticalSlicing.md`](SoftwareLearningGuide.Application/README.VerticalSlicing.md) | Organizacion por features, slices autonomos, Open/Closed en la practica |
| **Clean Architecture** | [`README.CleanArchitecture.md`](SoftwareLearningGuide.Api/README.CleanArchitecture.md) | Capas, regla de dependencias, Ports & Adapters, independencia tecnologica |

#### Patrones de Diseno

| Patron | Ubicacion | Descripcion |
|--------|-----------|-------------|
| **SOLID** | [`README.Pattern.SOLID.md`](SoftwareLearningGuide.Application/README.Pattern.SOLID.md) | Los 5 principios SOLID con ejemplos concretos del proyecto |
| **Factory** | [`README.Pattern.Factory.md`](SoftwareLearningGuide.Application/README.Pattern.Factory.md) | Static Factory Method en Value Objects y Agregados |
| **Builder** | [`README.Pattern.Builder.md`](SoftwareLearningGuide.Application/README.Pattern.Builder.md) | Builder fluido para tests, Commands complejos y records con `with` |
| **Repository** | [`README.Pattern.Repository.md`](SoftwareLearningGuide.Application/README.Pattern.Repository.md) | Abstraccion de acceso a datos, repositorios de escritura vs lectura |
| **Mediator** | [`README.Pattern.Mediator.md`](SoftwareLearningGuide.Application/README.Pattern.Mediator.md) | MediatR: Send vs Publish, Pipeline Behaviors, descubrimiento automatico |

#### Referentes de Tests

| Concepto | Ubicacion | Descripcion |
|----------|-----------|-------------|
| **TDD** | [`README.TDD.md`](tests/README.TDD.md) | Ciclo Red-Green-Refactor, buenas practicas de testing |
| **TestContainers** | [`README.TestContainer.md`](tests/README.TestContainer.md) | Contenedores Docker desechables para tests de integracion |
| **Respawn** | [`README.Respawn.md`](tests/README.Respawn.md) | Reseteo automatico de base de datos entre tests |
| **Response Fixture** | [`README.ResponseFixture.md`](tests/README.ResponseFixture.md) | Archivos JSON con respuestas esperadas para asserts |

#### Proveedores en la Nube

| Tema | Ubicacion | Descripcion |
|------|-----------|-------------|
| **AWS (General)** | [`README.AWS.md`](softwareLearningApplication/README.AWS.md) | Que es AWS, regiones, modelo de precios, catalogo de servicios con precios y comandos CLI |
| **MiniStack (Local AWS)** | [`README.Ministack.md`](softwareLearningApplication/README.Ministack.md) | Emulador local gratuito de AWS (60+ servicios), como funciona y su implementacion en este proyecto |

---

## Inicio Rapido

### Requisitos Previos

- **.NET 10 SDK** o superior
- **Docker Desktop** (para servicios de observabilidad y SQL Server)
- **Visual Studio 2026** (recomendado) o VS Code

### Ejecutar la API

El proyecto contiene **tres aplicaciones** que se ejecutan independientemente:

#### 1. API Principal (`SoftwareLearningGuide.Api`)

```bash
# Ejecuta la API REST
dotnet run --project SoftwareLearningGuide.Api

# O desde la carpeta del proyecto
cd SoftwareLearningGuide.Api
dotnet run

# O desde Visual Studio: F5
```

> **Puertos:** `http://localhost:5089` (HTTP) | `https://localhost:7033` (HTTPS)

#### 2. Outbox Processor (`SoftwareLearningGuide.OutboxProcessor`)

Worker Service que lee la tabla OutboxMessage y publica eventos a RabbitMQ. No contiene logica de consumo - solo publica.

```bash
# Ejecuta el Outbox Processor
dotnet run --project SoftwareLearningGuide.OutboxProcessor

# O desde la carpeta del proyecto
cd SoftwareLearningGuide.OutboxProcessor
dotnet run
```

> **Requiere:** SQL Server y RabbitMQ ejecutandose (`docker-compose up -d`)

#### 3. Consumer (`SoftwareLearningGuide.Consumer`)

Worker Service que escucha colas de RabbitMQ y procesa los eventos publicados por el OutboxProcessor.

```bash
# Ejecuta el Consumer
dotnet run --project SoftwareLearningGuide.Consumer

# O desde la carpeta del proyecto
cd SoftwareLearningGuide.Consumer
dotnet run
```

> **Requiere:** RabbitMQ ejecutandose (`docker-compose up -d`)

#### Endpoints Disponibles

**API Principal (`localhost:5089`):**

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| GET | `/api/v1.0/order` | Obtener todas las ordenes |
| GET | `/api/v1.0/order/{id}` | Obtener orden por GUID |
| POST | `/api/v1.0/order` | Crear una orden |
| GET | `/api/v1.0/product` | Obtener todos los productos |
| GET | `/api/v1.0/product/{id}` | Obtener producto por GUID |
| POST | `/api/v1.0/product` | Crear un producto |
| GET | `/api/v1.0/customer` | Obtener todos los clientes |
| GET | `/api/v1.0/customer/{id}` | Obtener cliente por GUID |
| POST | `/api/v1.0/customer` | Crear un cliente |
| GET | `/api/v1.0/weatherforecast` | Pronostico del clima (v1) |
| GET | `/api/v2.0/weatherforecast` | Pronostico del clima (v2, con metricas) |
| GET | `/api/v2.0/apirest` | Listar items (con filtro y paginacion) |
| GET | `/api/v2.0/apirest/{id}` | Obtener item por ID |
| POST | `/api/v2.0/apirest` | Crear item |
| PUT | `/api/v2.0/apirest/{id}` | Reemplazar item (actualizacion completa) |
| PATCH | `/api/v2.0/apirest/{id}` | Actualizar item (parcial) |
| DELETE | `/api/v2.0/apirest/{id}` | Eliminar item |
| POST | `/api/v2.0/apirest/upload` | Subir archivo (multipart, max 10MB) |
| GET | `/api/v2.0/apirest/stream` | Streaming de items (IAsyncEnumerable) |
| GET | `/api/v2.0/apirest/headers` | Leer header X-Request-ID |
| GET | `/api/v2.0/apirest/forbidden` | Ejemplo respuesta 403 |
| GET | `/api/v2.0/apirest/unauthorized` | Ejemplo respuesta 401 |
| GET | `/api/v2.0/apirest/problem` | Ejemplo respuesta 500 (ProblemDetails) |

**Documentacion:**
- **Scalar:** `https://localhost:7033/scalar/v1`
- **Swagger UI:** `https://localhost:7033/swagger`
- **OpenAPI Spec:** `https://localhost:7033/openapi/v1.json`

> **Nota:** Los controllers de Order, Product y Customer estan protegidos por Feature Flags. En modo Development todos estan habilitados.

---

## Docker Compose

El stack actual incluye:

- **Aspire Dashboard** - Panel de control para observabilidad de .NET
- **SQL Server 2022** - Base de datos para la aplicacion
- **Flagsmith** - Servicio de gestion de feature flags
- **PostgreSQL** - Base de datos de Flagsmith
- **RabbitMQ** - Message broker para integracion entre servicios

### Servicios Disponibles

| Servicio | URL | Puerto | Descripcion |
|----------|-----|--------|-------------|
| **Aspire Dashboard** | http://localhost:18888 | 18888 | Panel de trazas, metricas y logs |
| **OTEL Collector (gRPC)** | localhost:4317 | 4317 | Recibe telemetria de la app |
| **SQL Server** | localhost,1433 | 1433 | Base de datos SoftwareLearningGuide |
| **Flagsmith** | http://localhost:8000 | 8000 | Panel de feature flags |
| **PostgreSQL** | localhost:5432 | 5432 | Base de datos de Flagsmith |
| **RabbitMQ** | localhost:5672 | 5672 | Message broker (AMQP) |
| **RabbitMQ Management** | http://localhost:15672 | 15672 | UI de administracion de RabbitMQ (guest/guest) |

### Comandos Utiles

```bash
# Levantar el stack
docker-compose up -d

# Ver logs
docker-compose logs -f aspire-dashboard

# Detener el stack
docker-compose down

# Ver estado de servicios
docker-compose ps

# Conectar a SQL Server (desde la app)
# ConnectionString: Server=localhost,1433;Database=SoftwareLearningGuide;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;
```

---

## Objetivos de Aprendizaje

- Entender arquitectura REST con versionado
- Implementar Domain-Driven Design con Value Objects y Agregados
- Implementar Domain Events para desacoplamiento total en el dominio
- Usar el patron Transactional Outbox para atomicidad entre base de datos y mensajeria
- Usar el patron Result\<T\> para manejo de errores funcional
- Centralizar errores de dominio con DomainErrors para mensajes descriptivos y consistentes
- Implementar CQRS con MediatR para separar lecturas y escrituras
- Aplicar el patron Unit of Work para transaccionalidad y atomicidad en escrituras
- Usar Dapper para queries de lectura directa contra la base de datos
- Usar Entity Framework Core para commands con tracking y relaciones
- Implementar observabilidad moderna con OpenTelemetry (trazas, metricas, logs)
- Usar logging estructurado y scopes para correlacion de requests
- Exponer metricas personalizadas de negocio
- Documentar APIs con OpenAPI/Swagger
- Containerizar aplicaciones .NET con Docker
- Monitorear aplicaciones en produccion
- Gestionar feature toggles y flags con Microsoft.FeatureManagement y Flagsmith

---

## Estructura de Carpetas

```mermaid
graph TD
    Root["SoftwareLearningGuide/"]
    
    Core["Core.Business<br/>Capa de Dominio (DDD)"]
    CoreExc["Exceptions/<br/>Excepciones del dominio + Result&lt;T&gt;"]
    CoreErr["Errors/<br/>DomainErrors: catalogo centralizado"]
    CoreVO["ValueObjects/<br/>Money, Email, Address, OrderId"]
    CoreEnt["Entities/<br/>Product, Customer, OrderLine"]
    CoreAgg["Aggregates/<br/>Order (raiz del agregado)"]
    
    App["Application/<br/>Capa de Aplicacion"]
    AppQuery["Application.Query/<br/>Queries (Dapper, lectura)"]
    AppQueryGet["GetOrder/<br/>GetOrderQuery + Handler"]
    AppCmd["Application.Command/<br/>Commands (EF Core, escritura)"]
    AppCmdCreate["CreateOrder/<br/>CreateOrderCommand + Handler"]
    AppCmdPorts["Ports/<br/>IOrderWriteRepository, IUnitOfWork"]
    
    Infra["Infraestructure/<br/>Capa de Infraestructura"]
    InfraData["Infraestructure.Data/<br/>EF Core, DbContext"]
    InfraDataCtx["Context/<br/>ApplicationDbContext"]
    InfraDataConf["Configurations/<br/>OrderConfiguration"]
    InfraDataRepo["Repositories/<br/>OrderWriteRepository, UnitOfWork"]
    InfraPorts["Ports/<br/>IOrderWriteRepository"]
    
    Api["Api/<br/>API REST ASP.NET Core"]
    ApiCtrl["Controllers/<br/>OrderController, WeatherForecast"]
    ApiExt["Extensions/<br/>OpenTelemetry, ApiVersioning"]
    ApiFeat["FeatureToggles/<br/>Feature Management"]
    ApiMid["Middlewares/<br/>GlobalExceptionMiddleware"]
    ApiMet["Metrics/<br/>OrderMetrics"]
    
    Outbox["OutboxProcessor/<br/>Worker Service: Outbox → RabbitMQ"]
    Consumer["Consumer/<br/>Worker Service: RabbitMQ → Business Logic"]
    Docker["docker-compose.yml"]
    
    Root --> Core
    Root --> App
    Root --> Infra
    Root --> Api
    Root --> Outbox
    Root --> Consumer
    Root --> Docker
    
    Core --> CoreExc
    Core --> CoreErr
    Core --> CoreVO
    Core --> CoreEnt
    Core --> CoreAgg
    
    App --> AppQuery
    AppQuery --> AppQueryGet
    App --> AppCmd
    AppCmd --> AppCmdCreate
    AppCmd --> AppCmdPorts
    
    Infra --> InfraData
    InfraData --> InfraDataCtx
    InfraData --> InfraDataConf
    InfraData --> InfraDataRepo
    Infra --> InfraPorts
    
    Api --> ApiCtrl
    Api --> ApiExt
    Api --> ApiFeat
    Api --> ApiMid
    Api --> ApiMet
    
    style Root fill:#512BD4,color:#fff,stroke:#4020a6
    style Core fill:#4ECDC4,color:#fff
    style App fill:#FF6B6B,color:#fff
    style Infra fill:#45B7D1,color:#fff
    style Api fill:#8B5CF6,color:#fff
    style Outbox fill:#FF4655,color:#fff
    style Consumer fill:#FF6600,color:#fff
    style Docker fill:#2496ED,color:#fff
```

---

## Flujo Completo: API → Outbox → Consumer (Caso Crear Producto)

Esta seccion explica como interactuan los 3 artefactos del sistema cuando un cliente crea un producto. Es el ejemplo practico que integra todos los patrones del proyecto.

### Los 3 Artefactos

```mermaid
graph LR
    subgraph API["1. API"]
        API_DESC["Recibe HTTP, valida,<br/>persiste, genera eventos"]
    end
    
    subgraph Outbox["2. OutboxProcessor"]
        OUTBOX_DESC["Lee tabla OutboxMessage,<br/>publica a RabbitMQ"]
    end
    
    subgraph Consumer["3. Consumer"]
        CONSUMER_DESC["Escucha RabbitMQ,<br/>ejecuta logica de negocio externa"]
    end
    
    API --> Outbox
    Outbox --> Consumer
    
    style API fill:#8B5CF6,color:#fff
    style Outbox fill:#FF4655,color:#fff
    style Consumer fill:#FF6600,color:#fff
```

### Paso 1: El cliente envia POST /api/v1/product

El request llega al `ProductController`, que despacha un `CreateProductCommand` via MediatR:

```mermaid
sequenceDiagram
    participant C as Cliente
    participant PC as ProductController
    participant M as MediatR
    participant H as CreateProductCommandHandler
    
    C->>+PC: POST /api/v1/product
    Note over PC: FeatureGate check<br/>FT_ENABLE_PRODUCT_CONTROLLER<br/>FT_ENABLE_PRODUCT_CREATION
    PC->>+M: Send(CreateProductCommand)
    M->>+H: Handle()
    H-->>-M: Result
    M-->>-PC: Result
    PC-->>-C: 201 Created
```

### Paso 2: El Command Handler crea el dominio

El handler crea la entidad `Product` que **hereda de `ProduceEvents`**, validando reglas de negocio y disparando un domain event:

```csharp
// Product.cs hereda de ProduceEvents
var product = new Product(productId, "Laptop", "Gaming laptop",
    new Money(1200.00m, "USD"), 10);
// Internamente ejecuta:
//   AddDomainEvent(new ProductCreatedDomainEvent { ... })
// El evento queda en memoria dentro de la lista _domainEvents
```

### Paso 3: UnitOfWork persiste todo atomicamente

El handler llama a `_unitOfWork.SaveChangesAsync()` que hace dos cosas:

```mermaid
sequenceDiagram
    participant H as Handler
    participant UoW as UnitOfWork
    participant M as MediatR
    participant NH as NotificationHandler
    participant MT as MassTransit
    participant DB as SQL Server
    
    H->>+UoW: SaveChangesAsync()
    
    Note over UoW: 1. DispatchDomainEventsAsync()
    UoW->>UoW: Busca entidades ProduceEvents
    Note over UoW: Encuentra Product con<br/>ProductCreatedDomainEvent
    
    UoW->>+M: Publish(ProductCreatedDomainEvent)
    M->>+NH: Handle()
    NH->>NH: Crea ProductCreatedIntegrationEvent
    NH->>+MT: Publish(integrationEvent)
    Note over MT: MassTransit escribe en<br/>OutboxMessage<br/>(EN LA MISMA TRANSACCION SQL)
    MT-->>-NH: OK
    NH-->>-M: OK
    M-->>-UoW: OK
    
    Note over UoW: ClearDomainEvents()
    
    Note over UoW: 2. _context.SaveChangesAsync()
    UoW->>+DB: BEGIN TRANSACTION
    UoW->>DB: INSERT INTO Products (...)
    UoW->>DB: INSERT INTO OutboxMessage (...)
    UoW->>DB: COMMIT
    DB-->>-UoW: OK
    UoW-->>-H: OK
    
    Note over DB: ATOMICO: producto + mensaje juntos
```

**Clave:** El producto y el mensaje de outbox se guardan en la **misma transaccion SQL**. Si algo falla, no se guarda el producto y no se genera el mensaje.

### Paso 4: OutboxProcessor publica a RabbitMQ

El `OutboxProcessor` es un Worker Service que revisa la tabla `OutboxMessage` cada 5 segundos:

```mermaid
sequenceDiagram
    participant OP as OutboxProcessor
    participant DB as SQL Server
    participant MT as MassTransit
    participant RMQ as RabbitMQ
    
    loop Cada 5 segundos
        OP->>+DB: SELECT * FROM OutboxMessage WHERE Sent IS NULL
        DB-->>-OP: Registros pendientes
        Note over OP: Encuentra registro con<br/>ProductCreatedIntegrationEvent
        
        OP->>MT: Lee body del mensaje
        OP->>+MT: Publica a RabbitMQ
        MT->>+RMQ: Exchange: ProductCreatedIntegrationEvent
        Note over RMQ: Cola: product-created
        RMQ-->>-MT: OK
        MT-->>-OP: OK
        
        OP->>+DB: UPDATE OutboxMessage SET Sent = GETUTCDATE()
        DB-->>-OP: OK
    end
    
    Note over RMQ: Si RabbitMQ esta caido,<br/>OutboxProcessor reintenta<br/>automaticamente
```

**Nota:** Si RabbitMQ esta caido, el OutboxProcessor reintenta automaticamente cuando el broker se recupera. No se pierden mensajes.

### Paso 5: Consumer procesa el evento

El `ProductCreatedConsumer` en el servicio Consumer escucha la cola `product-created`:

```mermaid
sequenceDiagram
    participant RMQ as RabbitMQ
    participant C as ProductCreatedConsumer
    participant L as Logger
    participant EXT as Sistemas Externos
    
    RMQ->>+C: ProductCreatedIntegrationEvent
    Note over C: { ProductId: "...",<br/>Name: "Laptop",<br/>Price: 1200, Currency: "USD" }
    
    C->>+L: Log de confirmacion
    L-->>-C: OK
    
    par Logica de negocio externa
        C->>+EXT: Enviar email de bienvenida
        EXT-->>-C: OK
    and
        C->>+EXT: Actualizar cache de productos
        EXT-->>-C: OK
    and
        C->>+EXT: Notificar a sistema de inventario
        EXT-->>-C: OK
    end
    
    C-->>-RMQ: OK
```

### Diagrama Visual Completo

```mermaid
sequenceDiagram
    participant C as Cliente
    participant API as API
    participant SQL as SQL Server
    participant RMQ as RabbitMQ
    participant CON as Consumer
    
    C->>+API: POST /api/v1/product
    Note over API: ProductController<br/>FeatureGate check
    Note over API: CreateProductHandler<br/>new Product(...)<br/>AddDomainEvent(...)
    
    API->>API: UnitOfWork.SaveChangesAsy
    Note over API: DispatchDomainEventsAsync
    Note over API: IMediator.Publish()
    Note over API: NotificationHandler
    Note over API: MassTransit → OutboxMsg
    
    API->>+SQL: BEGIN TRANSACTION
    API->>SQL: INSERT INTO Products
    API->>SQL: INSERT INTO OutboxMsg
    API->>SQL: COMMIT
    SQL-->>-API: OK
    
    API-->>-C: 201 Created
    
    Note over API: OutboxProcessor (cada 5s)
    API->>+SQL: SELECT FROM OutboxMsg
    SQL-->>-API: Registros pendientes
    
    API->>+RMQ: Publica a RabbitMQ
    Note over RMQ: Cola product-created
    RMQ-->>-API: OK
    
    API->>+SQL: UPDATE Sent = NOW()
    SQL-->>-API: OK
    
    RMQ->>+CON: Consume mensaje
    Note over CON: ProductCreatedConsumer<br/>Log + logica externa
    CON-->>-RMQ: OK
```

### Que sucede si algo falla?

| Escenario | Resultado |
|-----------|-----------|
| Validacion falla en Product | No se crea nada, respuesta 400 con error |
| SQL Server caido | No se guarda, respuesta 500 (GlobalExceptionMiddleware) |
| RabbitMQ caido | Producto se guarda, OutboxMessage queda pendiente. OutboxProcessor reintenta |
| Consumer falla | Mensaje se queda en RabbitMQ con reintentos (at-least-once) |

### Documentacion Relacionada

Para profundizar en cada artefacto, consulta los READMEs dedicados:

- [`README.DDD.md`](SoftwareLearningGuide.Core.Business/README.DDD.md) - Entidad Product y su herencia de ProduceEvents
- [`README.EventSourcing.md`](SoftwareLearningGuide.Core.Business/README.EventSourcing.md) - Domain Events y su ciclo de vida
- [`README.UnitOfWork.md`](SoftwareLearningGuide.Infraestructure/README.UnitOfWork.md) - Como UnitOfWork despacha eventos y persiste
- [`README.OutboxPattern.md`](SoftwareLearningGuide.OutboxProcessor/README.OutboxPattern.md) - Transactional Outbox con MassTransit
- [`README.CQRS.md`](SoftwareLearningGuide.Application/README.CQRS.md) - Command/Query separation con MediatR

---

## Stack Tecnologico

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

## Soporte y Recursos

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

**Feliz aprendizaje!**
