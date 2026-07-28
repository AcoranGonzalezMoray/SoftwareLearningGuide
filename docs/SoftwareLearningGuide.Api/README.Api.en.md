# ASP.NET Core Web API - Controllers, Versioning and OpenAPI

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-10.0-purple)
![API](https://img.shields.io/badge/Pattern-REST_API-blue)

Controller-based REST API with **versioning**, **OpenAPI/Swagger** and **Scalar** in .NET 10.

---

## Table of Contents

1. [NuGet Packages](#nuget-packages)
2. [Fundamental Rule - Order in Program.cs](#fundamental-rule---order-in-programcs)
3. [Program.cs Configuration](#programcs-configuration)
4. [API Versioning](#api-versioning)
5. [Versioned Controllers](#versioned-controllers)
6. [OpenAPI Documentation](#openapi-documentation)
7. [REST Controller Example](#rest-controller-example)
8. [File Reference](#file-reference)

---

## NuGet Packages

| Package | Version | Usage |
|---------|---------|-------|
| `Microsoft.AspNetCore.OpenApi` | - | Native OpenAPI specification generator |
| `Swashbuckle.AspNetCore.SwaggerUI` | - | Swagger graphical interface |
| `Scalar.AspNetCore` | - | Modern graphical interface for OpenAPI |
| `Asp.Versioning.Mvc` | - | API versioning via controllers |
| `Asp.Versioning.Mvc.ApiExplorer` | - | Versioning + OpenAPI integration |

---

## Fundamental Rule - Order in Program.cs

In ASP.NET Core, the order within `Program.cs` is **critical**:

| Step | Description |
|------|-------------|
| **1. `builder.Services`** | All services are registered **before** `builder.Build()` |
| **2. `builder.Build()`** | Freezes the service container (read-only). Attempting to register services after throws `InvalidOperationException` |
| **3. `app.Use...` / `app.Map...`** | Configures the HTTP pipeline: middleware, endpoints, error middleware |

---

## Program.cs Configuration

> **Productive file:** [`SoftwareLearningGuide.Api/Program.cs`](SoftwareLearningGuide.Api/Program.cs)

`Program.cs` is the application entry point. This is where all services are **registered** and the HTTP pipeline is **configured**. The order is critical: first services (before `Build`), then the pipeline (after `Build`). Let's break down each section.

Before registering any service, it's important to understand that each registration in `builder.Services` depends on previous ones. For example, repositories need the `DbContext` (registered by `AddInfrastructure`), and the `DbContext` needs the connection string (loaded by `GetDatabaseOptions`). If you reverse the order, the application will fail to resolve dependencies.

```csharp
using Microsoft.FeatureManagement;
using SoftwareLearningGuide.Api.Extensions;
using SoftwareLearningGuide.Api.Middlewares;
using SoftwareLearningGuide.Api.Startup;
using SoftwareLearningGuide.Infraestructure;

namespace SoftwareLearningGuide.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================================================
            // 1. SERVICE REGISTRATION
            // =========================================================

            builder.Services.AddControllers();

            builder.Services.AddOptions(builder.Configuration);

            // Feature Flags: first load Flagsmith configuration,
            // then register the feature management engine
            builder.Configuration.AddFeatureManagementConfiguration(builder.Configuration);
            builder.Services.AddFeatureManagement();

            // OpenTelemetry: traces, metrics and structured logging.
            // Configured AFTER Features because it may depend on flags to enable/disable telemetry
            var otelOptions = builder.Configuration.GetOpenTelemetryOptions();
            builder.Services.AddCustomOpenTelemetry(builder.Logging, builder.Configuration, otelOptions);

            // API versioning and OpenAPI
            builder.Services.AddCustomApiVersioning();

            // CQRS: Infrastructure (EF Core DbContext + Domain OutboxWriter).
            // This registration MUST go before AddRepositories() because repos need DbContext injected
            var databaseOptions = builder.Configuration.GetDatabaseOptions();
            builder.Services.AddInfrastructure(databaseOptions.SoftwareLearningGuide);

            // CQRS: Repositories.
            // DEPENDS on the previous AddInfrastructure() — if you register it first, repos won't have DbContext
            builder.Services.AddRepositories();

            // CQRS: MediatR (IMediator + automatic handlers).
            // Registers all command and query handlers at once
            builder.Services.AddCQRS();

            // Custom metrics
            builder.Services.AddCustomMetrics();

            //DbConnections
            builder.Services.AddDbConnections(databaseOptions);

            // =========================================================
            // 2. APPLICATION BUILD
            // =========================================================

            var app = builder.Build();

            app.ApplyMigrations();

            // =========================================================
            // 3. HTTP REQUEST PIPELINE (MIDDLEWARES)
            // =========================================================

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "V1 Docs");
                    options.SwaggerEndpoint("/openapi/v2.json", "V2 Docs");
                });
            }

            // Global exception handling with logging scopes
            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
```

### Registration Order - Why It Matters

| Step | What it does | Why this order |
|------|--------------|----------------|
| `AddControllers()` | Registers controllers | Base for everything else |
| `AddOptions(config)` | Loads options from appsettings | OpenTelemetry needs them |
| `AddFeatureManagementConfiguration()` | Loads feature flags from Flagsmith | Before AddFeatureManagement |
| `AddFeatureManagement()` | Registers IFeatureManager | Needs the configuration already loaded |
| `AddCustomOpenTelemetry(...)` | Configures traces, metrics, logs | Before business logic |
| `AddCustomApiVersioning()` | Versioning + OpenAPI | Independent of the rest |
| `AddInfrastructure(connStr)` | EF Core DbContext + Outbox | Needs connection string |
| `AddRepositories()` | Write repositories | Depends on DbContext |
| `AddCQRS()` | CQRS handlers (command + query) | Unified method (not separate AddCommandHandlers/AddQueryHandlers) |
| `AddCustomMetrics()` | Business metrics | Singleton, no dependencies |
| `AddDbConnections(databaseOptions)` | IDbConnection for Dapper | Database options already loaded |
| `ApplyMigrations()` | Automatic migrations | Only in Development, after Build() |

### Why does order matter?

If you register `AddRepositories()` **before** `AddInfrastructure()`, the repositories wouldn't have the `DbContext` available and the app would fail on startup. It's like trying to use a car without an engine: all the code is there, but the fundamental piece isn't connected. ASP.NET Core resolves dependencies in the exact order you register them, so each service can only depend on what was already registered **before** it.

**Differences with previous versions:**

- Uses `AddCQRS()` (unified method) instead of separate `AddCommandHandlers()` and `AddQueryHandlers()`
- Uses `GetDatabaseOptions()` (typed options) instead of `GetConnectionString()`
- Feature Management is registered **before** OpenTelemetry
- `MapScalarApiReference()` is **not** in the pipeline (only `MapOpenApi()` + SwaggerUI)
- `ApplyMigrations()` runs after `builder.Build()`

---

## API Versioning

### AddCustomApiVersioning

> **Productive file:** [`SoftwareLearningGuide.Api/Extensions/ApiVersioningServiceCollectionExtensions.cs`](SoftwareLearningGuide.Api/Extensions/ApiVersioningServiceCollectionExtensions.cs)

API versioning allows multiple versions of an endpoint to coexist simultaneously. For this we use `Asp.Versioning.Mvc` which integrates with ASP.NET Core's routing system and with `AddApiExplorer` so that Swagger/OpenAPI can document each version separately.

The `AddCustomApiVersioning` method configures two aspects: the versioning engine (default version, behavior when unspecified) and the API explorer (group format in OpenAPI and URL version substitution). It also explicitly registers OpenAPI documents for v1 and v2.

```csharp
// Extensions/ApiVersioningServiceCollectionExtensions.cs
using Asp.Versioning;

namespace SoftwareLearningGuide.Api.Extensions
{
    public static class ApiVersioningServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddOpenApi("v1");
            services.AddOpenApi("v2");

            return services;
        }
    }
}
```

### Version Configuration

| Option | Value | Description |
|--------|-------|-------------|
| `DefaultApiVersion` | `1.0` | Default version when not specified |
| `AssumeDefaultVersionWhenUnspecified` | `true` | Uses default version when client doesn't specify |
| `ReportApiVersions` | `true` | Includes `api-supported-versions` headers in responses |
| `GroupNameFormat` | `'v'VVV` | Group format in OpenAPI: `v1`, `v2` |
| `SubstituteApiVersionInUrl` | `true` | Replaces `{version:apiVersion}` in the URL |

Now that we have versioning configured, let's see how it applies to controllers. Each controller is marked with a specific version and the framework routes to the correct controller based on the URL. If a client calls `/api/v1/weatherforecast`, it goes to the V1 controller; if it calls `/api/v2/weatherforecast`, it goes to V2.

---

## Versioned Controllers

### V1 Controller

> **Productive file:** [`SoftwareLearningGuide.Api/Controllers/WeatherForecastControllerExample/WeatherForecastController.cs`](SoftwareLearningGuide.Api/Controllers/WeatherForecastControllerExample/WeatherForecastController.cs)

The V1 controller demonstrates a minimally versioned endpoint. The key difference from a non-versioned controller is the use of `[ApiVersion("1.0")]` to bind it to a specific version, and the route `api/v{version:apiVersion}/[controller]` that allows the client to select the version via the URL.

The `Name` attribute on `[HttpGet]` is **mandatory** when there are multiple versions of the same controller, as it prevents `OperationId` conflicts in the OpenAPI specification. Each version must have a unique name so that Swagger/Scalar can differentiate them.

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace SoftwareLearningGuide.Api.Controllers.WeatherForecastControllerExample
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();
        }
    }
}
```

### Controller Key Points

| Attribute | Description |
|-----------|-------------|
| `[ApiVersion("1.0")]` | Marks the controller as version 1.0 |
| `[Route("api/v{version:apiVersion}/[controller]")]` | Versioned URL: `/api/v1/weatherforecast` |
| `[HttpGet(Name = "GetWeatherForecast")]` | **Unique Name** to prevent `OperationId` conflicts in OpenAPI |

---

## OpenAPI Documentation

### Available Endpoints

> **Note:** Ports and URLs correspond to `launchSettings.json` configuration. Actual values may vary depending on your development environment.

| Resource | URL |
|----------|-----|
| **OpenAPI V1 Specification** | `https://localhost:7033/openapi/v1.json` |
| **OpenAPI V2 Specification** | `https://localhost:7033/openapi/v2.json` |
| **Swagger UI** | `https://localhost:7033/swagger` |
| **Scalar** | `https://localhost:7033/scalar/v1` |

### launchSettings.json Configuration

> **Productive file:** [`SoftwareLearningGuide.Api/Properties/launchSettings.json`](SoftwareLearningGuide.Api/Properties/launchSettings.json)

`launchSettings.json` defines the development execution profiles. This is where HTTPS/HTTP ports, launch URL and environment variables are configured. Note that this project has **two profiles** (`https` and `http`), and the launch URL (`launchUrl`) points to Swagger UI by default, not Scalar directly.

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5089",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7033;http://localhost:5089",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Once we have the documented API, the next step is to implement the business logic. Let's see an example controller that demonstrates CQRS with MediatR.

---

## REST Controller Example

> **Productive file:** [`SoftwareLearningGuide.Api/Controllers/APIRESTControllerExample/APIRESTController.cs`](SoftwareLearningGuide.Api/Controllers/APIRESTControllerExample/APIRESTController.cs)

The project includes an example controller in `Controllers/APIRESTControllerExample/APIRESTController.cs` that demonstrates complete CRUD operations, including filters, pagination, file upload and streaming. This controller is versioned as **v2**, so all endpoints use the `/api/v2/apirest` prefix.

This controller is the counterpart to V1: while the WeatherForecastController V1 is a simple read-only endpoint, this one demonstrates all HTTP operations available in a real REST API.

| Feature | Endpoint |
|---------|----------|
| **Query params** | `GET /api/v2/apirest?q=apple&page=1&pageSize=10` |
| **Route with ID** | `GET /api/v2/apirest/{id}` |
| **Custom headers** | `X-Request-ID` via `[FromHeader]` |
| **POST create** | `POST /api/v2/apirest` |
| **PUT replace** | `PUT /api/v2/apirest/{id}` |
| **PATCH partial** | `PATCH /api/v2/apirest/{id}` |
| **DELETE delete** | `DELETE /api/v2/apirest/{id}` |
| **File upload** | `POST /api/v2/apirest/upload` (multipart form-data) |
| **Streaming** | `GET /api/v2/apirest/stream` (IAsyncEnumerable) |

### curl Examples

> **Note:** Use `https://localhost:7033` as base URL instead of `7000` (the old port). All endpoints include the version prefix `/api/v2/`.

```bash
# List
curl -sS "https://localhost:7033/api/v2/apirest" -k

# Get by ID
curl -sS "https://localhost:7033/api/v2/apirest/1" -k

# Create
curl -sS -X POST "https://localhost:7033/api/v2/apirest" \
  -H "Content-Type: application/json" \
  -d '{"name":"Kiwi","description":"Fruit"}' -k

# Upload file
curl -sS -X POST "https://localhost:7033/api/v2/apirest/upload" \
  -F "file=@./my-file.txt" -k
```

---

## File Reference

| File | Description |
|------|-------------|
| `SoftwareLearningGuide.Api/Program.cs` | Service registration and HTTP pipeline |
| `SoftwareLearningGuide.Api/Extensions/ApiVersioningServiceCollectionExtensions.cs` | Versioning and OpenAPI configuration |
| `SoftwareLearningGuide.Api/Extensions/OpenTelemetryServiceCollectionExtensions.cs` | OpenTelemetry configuration |
| `SoftwareLearningGuide.Api/Extensions/OptionConfigurationServiceCollectionExtensions.cs` | Options loading from appsettings |
| `SoftwareLearningGuide.Api/Extensions/ApplicationBuilderExtensions.cs` | Application middleware (migrations) |
| `SoftwareLearningGuide.Api/Extensions/ConfigurationBuilderExtensions.cs` | Feature Flags configuration from Flagsmith |
| `SoftwareLearningGuide.Api/Options/OpenTelemetryOptions.cs` | OTEL options model |
| `SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs` | CQRS Controller (GET/POST orders) |
| `SoftwareLearningGuide.Api/Metrics/OrderMetrics.cs` | Custom business metric |
| `SoftwareLearningGuide.Api/Startup/CqrsStartup.cs` | MediatR registration (unified handlers) |
| `SoftwareLearningGuide.Api/Startup/ReporitoriesStartup.cs` | Repository registration |
| `SoftwareLearningGuide.Api/Startup/DbConnectionsStartup.cs` | IDbConnection registration |
| `SoftwareLearningGuide.Api/Startup/MetricsStartup.cs` | Metrics registration in DI |