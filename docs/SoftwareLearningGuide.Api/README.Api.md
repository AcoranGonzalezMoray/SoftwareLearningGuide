# ASP.NET Core Web API - Controladores, Versionado y OpenAPI

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-10.0-purple)
![API](https://img.shields.io/badge/Pattern-REST_API-blue)

API REST basada en controladores con **versionado**, **OpenAPI/Swagger** y **Scalar** en .NET 10.

---

#### Tabla de Contenidos

1. [Paquetes NuGet](#paquetes-nuget)
2. [Regla Fundamental - Orden en Program.cs](#regla-fundamental---orden-en-programcs)
3. [Configuración de Program.cs](#configuración-de-programcs)
4. [Versionado de APIs](#versionado-de-apis)
5. [Controladores Versionados](#controladores-versionados)
6. [Documentación OpenAPI](#documentación-openapi)
7. [REST Controller de Ejemplo](#rest-controller-de-ejemplo)
8. [Referencia de Archivos](#referencia-de-archivos)

---

## Paquetes NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Microsoft.AspNetCore.OpenApi` | - | Generador de especificación OpenAPI nativo |
| `Swashbuckle.AspNetCore.SwaggerUI` | - | Interfaz gráfica Swagger |
| `Scalar.AspNetCore` | - | Interfaz gráfica moderna para OpenAPI |
| `Asp.Versioning.Mvc` | - | Versionado de APIs mediante controladores |
| `Asp.Versioning.Mvc.ApiExplorer` | - | Integración versionado + OpenAPI |

---

## Regla Fundamental - Orden en Program.cs

En ASP.NET Core, el orden dentro de `Program.cs` es **crítico**:

| Paso | Descripción |
|------|-------------|
| **1. `builder.Services`** | Todos los servicios se registran **antes** de `builder.Build()` |
| **2. `builder.Build()`** | Congela el contenedor de servicios (solo lectura). Intentar registrar servicios después lanza `InvalidOperationException` |
| **3. `app.Use...` / `app.Map...`** | Configura el pipeline HTTP: middlewares, endpoints, middlewares de error |

---

## Configuración de Program.cs

> **Archivo productivo:** [`SoftwareLearningGuide.Api/Program.cs`](SoftwareLearningGuide.Api/Program.cs)

`Program.cs` es el punto de entrada de la aplicación. Aquí es donde se **registran** todos los servicios y se **configura** el pipeline HTTP. El orden es crítico: primero los servicios (antes de `Build`), luego el pipeline (después de `Build`). Vamos a desglosar cada sección.

Antes de registrar cualquier servicio, es importante entender que cada registro en `builder.Services` depende de los anteriores. Por ejemplo, los repositorios necesitan el `DbContext` (que registra `AddInfrastructure`), y el `DbContext` necesita la cadena de conexión (que carga `GetDatabaseOptions`). Si inviertes el orden, la aplicación fallará al resolver dependencias.

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
            // 1. REGISTRO DE SERVICIOS
            // =========================================================

            builder.Services.AddControllers();

            builder.Services.AddOptions(builder.Configuration);

            // Feature Flags: primero cargamos la configuración de Flagsmith,
            // luego registramos el motor de feature management
            builder.Configuration.AddFeatureManagementConfiguration(builder.Configuration);
            builder.Services.AddFeatureManagement();

            // OpenTelemetry: trazas, métricas y logging estructurado.
            // Se configura DESPUÉS de Features porque puede depender de flags para habilitar/deshabilitar telemetría
            var otelOptions = builder.Configuration.GetOpenTelemetryOptions();
            builder.Services.AddCustomOpenTelemetry(builder.Logging, builder.Configuration, otelOptions);

            // Versionado de APIs y OpenAPI
            builder.Services.AddCustomApiVersioning();

            // CQRS: Infrastructure (EF Core DbContext + Domain OutboxWriter).
            // Este registro DEBE ir antes de AddRepositories() porque los repos necesitan el DbContext inyectado
            var databaseOptions = builder.Configuration.GetDatabaseOptions();
            builder.Services.AddInfrastructure(databaseOptions.SoftwareLearningGuide);

            // CQRS: Repositories.
            // DEPENDEN de AddInfrastructure() anterior — si lo registras antes, los repos no tendrán DbContext
            builder.Services.AddRepositories();

            // CQRS: MediatR (IMediator + handlers automáticamente).
            // Registra todos los handlers de commands y queries de golpe
            builder.Services.AddCQRS();

            // Métricas custom
            builder.Services.AddCustomMetrics();

            //DbConnections
            builder.Services.AddDbConnections(databaseOptions);

            // =========================================================
            // 2. CONSTRUCCION DE LA APLICACION
            // =========================================================

            var app = builder.Build();

            app.ApplyMigrations();

            // =========================================================
            // 3. PIPELINE DE PETICIONES HTTP (MIDDLEWARES)
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

            // Exception handling global con scopes de logging
            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
```

### Orden de Registro - Por que Importa

| Paso | Que hace | Por que este orden |
|------|----------|-------------------|
| `AddControllers()` | Registra controladores | Base para todo lo demas |
| `AddOptions(config)` | Carga opciones desde appsettings | OpenTelemetry las necesita |
| `AddFeatureManagementConfiguration()` | Carga feature flags desde Flagsmith | Antes de AddFeatureManagement |
| `AddFeatureManagement()` | Registra IFeatureManager | Necesita el configuration ya cargado |
| `AddCustomOpenTelemetry(...)` | Configura trazas, metricas, logs | Antes de logica de negocio |
| `AddCustomApiVersioning()` | Versionado + OpenAPI | Independiente del resto |
| `AddInfrastructure(connStr)` | EF Core DbContext + Outbox | Necesita connection string |
| `AddRepositories()` | Repos de escritura | Depende de DbContext |
| `AddCQRS()` | Handlers de CQRS (command + query) | Metodo unificado (no AddCommandHandlers/AddQueryHandlers separados) |
| `AddCustomMetrics()` | Metricas de negocio | Singleton, sin dependencias |
| `AddDbConnections(databaseOptions)` | IDbConnection para Dapper | Opciones de BD ya cargadas |
| `ApplyMigrations()` | Migraciones automaticas | Solo en Development, despues de Build() |

### ¿Por qué importa el orden?

Si registras `AddRepositories()` **antes** de `AddInfrastructure()`, los repositorios no tendrían el `DbContext` disponible y la app fallaría al arrancar. Es como intentar usar un coche sin motor: todo el código está ahí, pero la pieza fundamental no está conectada. ASP.NET Core resuelve las dependencias en el orden exacto en que las registras, así que cada servicio solo puede depender de lo que ya se registró **antes** de él.

**Diferencias con versiones anteriores:**

- Se usa `AddCQRS()` (metodo unificado) en lugar de `AddCommandHandlers()` y `AddQueryHandlers()` por separado
- Se usa `GetDatabaseOptions()` (opciones tipadas) en lugar de `GetConnectionString()`
- Feature Management se registra **antes** de OpenTelemetry
- `MapScalarApiReference()` **no** esta en el pipeline (solo `MapOpenApi()` + SwaggerUI)
- `ApplyMigrations()` se ejecuta despues de `builder.Build()`

---

## Versionado de APIs

### AddCustomApiVersioning

> **Archivo productivo:** [`SoftwareLearningGuide.Api/Extensions/ApiVersioningServiceCollectionExtensions.cs`](SoftwareLearningGuide.Api/Extensions/ApiVersioningServiceCollectionExtensions.cs)

El versionado de APIs permite que múltiples versiones de un endpoint coexistan simultáneamente. Para ello usamos `Asp.Versioning.Mvc` que se integra con el sistema de enrutamiento de ASP.NET Core y con `AddApiExplorer` para que Swagger/OpenAPI pueda documentar cada versión por separado.

El método `AddCustomApiVersioning` configura dos aspectos: el motor de versionado (versión por defecto, comportamiento cuando no se especifica) y el explorador de API (formato del grupo en OpenAPI y sustitución de la versión en la URL). También registra explícitamente los documentos OpenAPI para v1 y v2.

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

### Configuración de Versiones

| Opción | Valor | Descripción |
|--------|-------|-------------|
| `DefaultApiVersion` | `1.0` | Versión por defecto si no se especifica |
| `AssumeDefaultVersionWhenUnspecified` | `true` | Usa la versión por defecto cuando el cliente no especifica |
| `ReportApiVersions` | `true` | Incluye headers `api-supported-versions` en las respuestas |
| `GroupNameFormat` | `'v'VVV` | Formato del grupo en OpenAPI: `v1`, `v2` |
| `SubstituteApiVersionInUrl` | `true` | Reemplaza `{version:apiVersion}` en la URL |

Ahora que tenemos el versionado configurado, veamos cómo se aplica a los controladores. Cada controlador se marca con una versión específica y el framework se encarga de enrutar al controlador correcto según la URL. Si un cliente llama a `/api/v1/weatherforecast`, va al controller V1; si llama a `/api/v2/weatherforecast`, va al V2.

---

## Controladores Versionados

### Controlador V1

> **Archivo productivo:** [`SoftwareLearningGuide.Api/Controllers/WeatherForecastControllerExample/WeatherForecastController.cs`](SoftwareLearningGuide.Api/Controllers/WeatherForecastControllerExample/WeatherForecastController.cs)

El controller V1 demuestra un endpoint mínimamente versionado. La diferencia clave con un controller no versionado es el uso de `[ApiVersion("1.0")]` para vincularlo a una versión específica, y la ruta `api/v{version:apiVersion}/[controller]` que permite al cliente seleccionar la versión vía la URL.

El atributo `Name` en `[HttpGet]` es **obligatorio** cuando hay múltiples versiones del mismo controller, ya que evita冲突 de `OperationId` en la especificación OpenAPI. Cada versión debe tener un nombre único para que Swagger/ Scalar pueda diferenciarlas.

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

### Puntos Clave del Controlador

| Atributo | Descripción |
|----------|-------------|
| `[ApiVersion("1.0")]` | Marca el controlador como versión 1.0 |
| `[Route("api/v{version:apiVersion}/[controller]")]` | URL versionada: `/api/v1/weatherforecast` |
| `[HttpGet(Name = "GetWeatherForecast")]` | **Name único** para evitar conflictos de `OperationId` en OpenAPI |

---

## Documentación OpenAPI

### Endpoints Disponibles

> **Nota:** Los puertos y URLs corresponden a la configuración de `launchSettings.json`. Los valores reales pueden variar según tu entorno de desarrollo.

| Recurso | URL |
|---------|-----|
| **Especificación OpenAPI V1** | `https://localhost:7033/openapi/v1.json` |
| **Especificación OpenAPI V2** | `https://localhost:7033/openapi/v2.json` |
| **Swagger UI** | `https://localhost:7033/swagger` |
| **Scalar** | `https://localhost:7033/scalar/v1` |

### Configuración de launchSettings.json

> **Archivo productivo:** [`SoftwareLearningGuide.Api/Properties/launchSettings.json`](SoftwareLearningGuide.Api/Properties/launchSettings.json)

`launchSettings.json` define los perfiles de ejecución en desarrollo. Es aquí donde se configuran los puertos HTTPS/HTTP, la URL de lanzamiento y las variables de entorno. Observa que en este proyecto hay **dos perfiles** (`https` y `http`), y la URL de lanzamiento (`launchUrl`) apunta a Swagger UI por defecto, no a Scalar directamente.

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

Una vez que tenemos la API documentada, el siguiente paso es implementar la lógica de negocio. Veamos un controller de ejemplo que demuestra CQRS con MediatR.

---

## REST Controller de Ejemplo

> **Archivo productivo:** [`SoftwareLearningGuide.Api/Controllers/APIRESTControllerExample/APIRESTController.cs`](SoftwareLearningGuide.Api/Controllers/APIRESTControllerExample/APIRESTController.cs)

El proyecto incluye un controller de ejemplo en `Controllers/APIRESTControllerExample/APIRESTController.cs` que demuestra las operaciones CRUD completas, incluyendo filtros, paginación, upload de ficheros y streaming. Este controlador está versionado como **v2**, por lo que todos los endpoints usan el prefijo `/api/v2/apirest`.

Este controller es el contrapunto del V1: mientras el WeatherForecastController V1 es un endpoint simple de solo lectura, este demuestra todas las operaciones HTTP disponibles en una API REST real.

| Característica | Endpoint |
|----------------|----------|
| **Query params** | `GET /api/v2/apirest?q=manzana&page=1&pageSize=10` |
| **Ruta con ID** | `GET /api/v2/apirest/{id}` |
| **Headers custom** | `X-Request-ID` via `[FromHeader]` |
| **POST crear** | `POST /api/v2/apirest` |
| **PUT reemplazar** | `PUT /api/v2/apirest/{id}` |
| **PATCH parcial** | `PATCH /api/v2/apirest/{id}` |
| **DELETE borrar** | `DELETE /api/v2/apirest/{id}` |
| **Upload ficheros** | `POST /api/v2/apirest/upload` (multipart form-data) |
| **Streaming** | `GET /api/v2/apirest/stream` (IAsyncEnumerable) |

### Ejemplos con curl

> **Nota:** Usa `https://localhost:7033` como base URL en lugar de `7000` (que era el puerto antiguo). Todos los endpoints incluyen el prefijo de versión `/api/v2/`.

```bash
# Listar
curl -sS "https://localhost:7033/api/v2/apirest" -k

# Obtener por ID
curl -sS "https://localhost:7033/api/v2/apirest/1" -k

# Crear
curl -sS -X POST "https://localhost:7033/api/v2/apirest" \
  -H "Content-Type: application/json" \
  -d '{"name":"Kiwi","description":"Fruta"}' -k

# Subir fichero
curl -sS -X POST "https://localhost:7033/api/v2/apirest/upload" \
  -F "file=@./mi-fichero.txt" -k
```

---

## Referencia de Archivos

| Archivo | Descripción |
|---------|-------------|
| `SoftwareLearningGuide.Api/Program.cs` | Registro de servicios y pipeline HTTP |
| `SoftwareLearningGuide.Api/Extensions/ApiVersioningServiceCollectionExtensions.cs` | Configuración de versionado y OpenAPI |
| `SoftwareLearningGuide.Api/Extensions/OpenTelemetryServiceCollectionExtensions.cs` | Configuración de OpenTelemetry |
| `SoftwareLearningGuide.Api/Extensions/OptionConfigurationServiceCollectionExtensions.cs` | Carga de opciones desde appsettings |
| `SoftwareLearningGuide.Api/Extensions/ApplicationBuilderExtensions.cs` | Middleware de la aplicación (migraciones) |
| `SoftwareLearningGuide.Api/Extensions/ConfigurationBuilderExtensions.cs` | Configuración de Feature Flags desde Flagsmith |
| `SoftwareLearningGuide.Api/Options/OpenTelemetryOptions.cs` | Modelo de opciones OTEL |
| `SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs` | Controller CQRS (GET/POST orders) |
| `SoftwareLearningGuide.Api/Metrics/OrderMetrics.cs` | Métrica personalizada de negocio |
| `SoftwareLearningGuide.Api/Startup/CqrsStartup.cs` | Registro de MediatR (handlers unificados) |
| `SoftwareLearningGuide.Api/Startup/ReporitoriesStartup.cs` | Registro de repositories |
| `SoftwareLearningGuide.Api/Startup/DbConnectionsStartup.cs` | Registro de IDbConnection |
| `SoftwareLearningGuide.Api/Startup/MetricsStartup.cs` | Registro de métricas en DI |