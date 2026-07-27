# Feature Management - Feature Toggles y Feature Flags

![FeatureManagement](https://img.shields.io/badge/Library-Microsoft.FeatureManagement-blue)
![Flagsmith](https://img.shields.io/badge/Service-Flagsmith-green)

**Feature Management** permite controlar la ejecucion de funcionalidades en tiempo de ejecucion sin volver a desplegar la aplicacion. En este proyecto usamos **Microsoft.FeatureManagement** integrado con **Flagsmith** (servicio externo en Docker) para gestionar toggles de forma centralizada con soporte de **hot reload**.

---

## Tabla de Contenidos

1. [Feature Toggle vs Feature Flag](#feature-toggle-vs-feature-flag)
2. [Feature Gate - Control a Nivel de Atributo](#feature-gate---control-a-nivel-de-atributo)
3. [Feature Hot Reload - Cambios en Caliente](#feature-hot-reload---cambios-en-caliente)
4. [Arquitectura en Este Proyecto](#arquitectura-en-este-proyecto)
5. [Docker - Flagsmith](#docker---flagsmith)
6. [ConfigurationProvider Personalizado](#configurationprovider-personalizado)
7. [Seccion FeatureManagement en appsettings](#seccion-featuremanagement-en-appsettings)
8. [Uso con IFeatureManager](#uso-con-ifeaturemanager)
9. [Constantes - FeatureToggleNames](#constantes---featuretogglenames)
10. [Diagrama de Flujo](#diagrama-de-flujo)
11. [Referencia de Archivos](#referencia-de-archivos)
12. [Paquetes NuGet](#paquetes-nuget)

---

## Feature Toggle vs Feature Flag

Aunque los terminos se usan indistintamente en la industria, tienen diferencias conceptuales importantes:

### Feature Toggle (Feature Toggle)

Un **Feature Toggle** es un mecanismo que permite **activar o desactivar funcionalidades** en tiempo de ejecucion. Se usa principalmente para:

| Uso | Descripcion | Ejemplo |
|-----|-------------|---------|
| **Release Toggle** | Liberar funcionalidades gradualmente a usuarios | Nuevo boton de pago visible solo para 10% de usuarios |
| **Experiment Toggle** | Ejecutar experimentos A/B | Mostrar layout A o layout B para medir conversion |
| **Ops Toggle** | Controlar comportamiento operacional | Activar/desactivar cache, circuit breakers |
| **Permission Toggle** | Controlar acceso por rol o mercado | Funcionalidad disponible solo en EU |

**Filosofia:** El feature toggle esta ligado al **ciclo de vida de la funcionalidad**. Se crea, se prueba y se libera gradualmente.

```csharp
// Ejemplo: Release Toggle - liberar nueva funcionalidad gradualmente
if (await featureManager.IsEnabledAsync("FT_ENABLE_ORDER_CREATION"))
{
    // Logica nueva que solo se ejecuta si el toggle esta activo
}
```

### Feature Flag

Un **Feature Flag** permite realizar **parallel changes** de forma segura, sin releaser. Se usa para:

| Uso | Descripcion | Ejemplo |
|-----|-------------|---------|
| **Migration Flag** | Migrar entre dos implementaciones sin downtime | Base de datos vieja a nueva, sin corte de servicio |
| **Deployment Flag** | Desplegar codigo sin activarlo | Codigo desplegado en prod pero deshabilitado |
| **Kill Switch** | Desactivar rapidamente una funcionalidad problematica | Si el servicio de pagos falla, desactivar pagos inmediatamente |
| **Permission Flag** | Controlar acceso por entorno o configuracion | Feature disponible en Development pero no en Production |

**Filosofia:** El feature flag esta ligado al **despliegue y la operacion**. Permite desplegar codigo de forma segura, migrar entre implementaciones, mantener la estabilidad del sistema y eventualmente se elimina el flag (se queda el codigo habilitado).

```csharp
// Ejemplo: Migration Flag - migrar entre dos implementaciones
if (await featureManager.IsEnabledAsync("FT_USE_NEW_PAYMENT_GATEWAY"))
{
    return await _newPaymentGateway.ProcessAsync(order);
}
else
{
    return await _legacyPaymentGateway.ProcessAsync(order);
}
```

### Resumen Visual

```
Feature Toggle                          Feature Flag
─────────────────────────────           ─────────────────────────────
Liberar funcionalidades                 Parallel changes / Migraciones
│                                       │
├─ Release gradual a usuarios           ├─ Migracion sin downtime
├─ Experimentos A/B                     ├─ Despliegue seguro
├─ Control operacional                  ├─ Kill switch
└─ Control por rol/mercado              └─ Control por entorno

Vinculado al: Ciclo de vida             Vinculado al: Despliegue
de la funcionalidad                     y la operacion
```

> **La diferencia fundamental es el propósito:** un Toggle libera funcionalidades gradualmente (como un grifo que abres poco a poco), mientras que un Flag permite migraciones seguras (como un interruptor que cambias de una pared a otra). En la práctica, muchos usos se superponen, pero entender la diferencia te ayuda a elegir la herramienta correcta.

---

## Feature Gate - Control a Nivel de Atributo

`Microsoft.FeatureManagement.AspNetCore` proporciona el atributo `[FeatureGate]` que permite habilitar o deshabilitar **actions de controllers** o **controllers enteros** directamente con un atributo, sin necesidad de escribir `if/else` en el codigo.

### Como Funciona

Cuando un request llega a un endpoint, el middleware de Feature Management evalua el atributo `[FeatureGate]`:
- Si el feature esta **habilitado** → ejecuta el action normalmente
- Si el feature esta **deshabilitado** → retorna `404 Not Found` (por defecto)

### Ejemplo en Este Proyecto

```csharp
// Controllers/OrderControllerExample/OrderController.cs

[ApiController]
[ApiVersion("1.0")]
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CONTROLLER)]  // ← Controla todo el controller
[Route("api/v{version:apiVersion}/[controller]")]
public class OrderController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_RETRIEVAL)]  // ← Controla solo este action
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        // ...
    }

    [HttpPost]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CREATION)]  // ← Controla solo este action
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // ...
    }
}
```

### Niveles de Control

| Nivel | Atributo | Efecto |
|-------|----------|--------|
| **Controller** | `[FeatureGate("FT_X")]` en la clase | Si FT_X esta off, TODO el controller retorna 404 |
| **Action** | `[FeatureGate("FT_X")]` en el metodo | Si FT_X esta off, solo ese metodo retorna 404 |

### Flujo con FeatureGate

```
Request: GET /api/v1/order/{id}
    │
    ▼
[FeatureGate(FT_ENABLE_ORDER_RETRIEVAL)] se evalua
    │
    ├─ FT_ENABLE_ORDER_RETRIEVAL = true  → Ejecuta GetById()
    │
    └─ FT_ENABLE_ORDER_RETRIEVAL = false → Retorna 404 Not Found
```

### Uso Multiple (AND/OR)

```csharp
// AND: Ambos features deben estar activos
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CONTROLLER, FeatureToggleNames.FT_ENABLE_ORDER_CREATION)]
public async Task<IActionResult> Create(...) { }

// OR: Al menos uno debe estar activo (usando FeatureGateFilter)
```

---

## Feature Hot Reload - Cambios en Caliente

El **Feature Hot Reload** permite que los feature flags se actualicen en tiempo real sin reiniciar la aplicacion. Cuando cambias un flag en Flagsmith, la aplicacion lo refleja automaticamente en los proximos segundos.

### Como Funciona

```
Flagsmith (Docker)          Aplicacion .NET
─────────────────          ─────────────────
                           Load() + Timer cada N segundos
                               │
    Cambias un flag            │
    en el panel web            │
         │                     │
         │    FetchFlags()     │
         ◄─────────────────────│  ← Polling periodico
         │                     │
         │    OnReload()       │
         │────────────────────►│  ← Notifica a IConfiguration
                               │
                               │  IFeatureManager lee nuevos valores
                               │
    GET /api/v1/order          │
    ──────────────────────────►│  ← Feature evaluado con valor actualizado
         │                     │
         ◄─────────────────────│
    Response con nuevo         │
    comportamiento             │
```

### Configuracion

```json
// appsettings.Development.json
{
  "FeatureManagementApiConfiguration": {
    "ApiUrl": "http://localhost:8000/api/v1/flags/",
    "ApiKey": "6xGqbGoN5rfSkAg59vnvr2",
    "ReloadIntervalSeconds": 3
  }
}
```

| Parametro | Descripcion | Valor por defecto |
|-----------|-------------|-------------------|
| `ReloadIntervalSeconds` | Cada cuantos segundos se consulta Flagsmith | `30` |
| `ReloadIntervalSeconds = 0` | Desactiva el polling (solo carga al arrancar) | - |

### Ejemplo Practico

```
1. App arranca con FT_ENABLE_ORDER_CREATION = true
2. Creas una orden → funciona OK
3. En Flagsmith, desactivas FT_ENABLE_ORDER_CREATION
4. Esperas 3 segundos (ReloadIntervalSeconds)
5. Intentas crear otra orden → 404 Not Found (sin reiniciar la app)
```

### IFeatureManagerSnapshot vs IFeatureManager

```csharp
// IFeatureManager: lee valores frescos en cada llamada (recomendado para hot reload)
private readonly IFeatureManager _featureManager;

// IFeatureManagerSnapshot: cachea el valor por request (mas rapido pero no ve cambios instantaneos)
private readonly IFeatureManagerSnapshot _featureManager;
```

`Microsoft.FeatureManagement` inyecta automaticamente `IFeatureManagerSnapshot` en controllers, que captura el valor del flag al inicio del request. Esto garantiza consistencia durante un solo request.

---

## Arquitectura en Este Proyecto

La implementacion usa una arquitectura de **doble fuente** con prioridad y **hot reload**:

```
Fuente externa (Flagsmith en Docker)
    │
    │  Prioridad ALTA (sobreescribe)
    ▼
┌──────────────────────────────────────────────────────┐
│               Microsoft IConfiguration               │
│                                                      │
│  FeatureManagement:FT_ENABLE_ORDER_CREATION = true   │  ← Flagsmith (externo)
│  FeatureManagement:FT_ENABLE_ORDER_CREATION = false  │  ← appsettings (interno)
│                                                      │
│  El ultimo provider registrado gana.                 │
│  Flagsmith se registra DESPUES de appsettings        │
│                                                      │
│  Timer cada N segundos → FetchFlags() → OnReload()   │  ← Hot Reload
└──────────────────────────────────────────────────────┘
```

**Flujo de carga:**

1. `appsettings.json` carga valores por defecto (FeatureManagement section)
2. `appsettings.Development.json` sobreescribe con valores de Development
3. `FeatureManagementConfigurationProvider` consulta Flagsmith y sobreescribe lo que encuentre
4. Cada `ReloadIntervalSeconds`, el provider re-consulta Flagsmith y notifica cambios via `OnReload()`

Si Flagsmith no esta disponible o la URL esta vacia, se usan los valores de appsettings.

---

## Docker - Flagsmith

El stack Docker incluye **Flagsmith** como servicio de gestion de feature flags con PostgreSQL como backend.

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:15-alpine
    container_name: flagsmith-postgres
    environment:
      POSTGRES_DB: flagsmith
      POSTGRES_USER: flagsmith
      POSTGRES_PASSWORD: flagsmith_password
    ports:
      - "5432:5432"

  flagsmith:
    image: flagsmith/flagsmith:latest
    container_name: flagsmith
    environment:
      DATABASE_URL: postgres://flagsmith:flagsmith_password@postgres:5432/flagsmith
      PORT: 8000
    ports:
      - "8000:8000"
    depends_on:
      - postgres
```

### Servicios

| Servicio | URL | Puerto | Descripcion |
|----------|-----|--------|-------------|
| **Flagsmith** | http://localhost:8000 | 8000 | Panel de administracion y API |
| **PostgreSQL** | localhost:5432 | 5432 | Base de datos de Flagsmith |

### Stack Docker Completo

El `docker-compose.yml` completo incluye servicios adicionales para SQL Server, RabbitMQ y Aspire Dashboard:

```yaml
# docker-compose.yml
version: '3.8'

name: SoftwareLearningGuide

services:
  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    container_name: aspire-dashboard
    ports:
      - "18888:18888"
      - "4317:18889"
    restart: unless-stopped

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver-slg
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong!Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    restart: unless-stopped

  postgres:
    image: postgres:15-alpine
    container_name: flagsmith-postgres
    environment:
      POSTGRES_DB: flagsmith
      POSTGRES_USER: flagsmith
      POSTGRES_PASSWORD: flagsmith_password
    ports:
      - "5432:5432"
    volumes:
      - flagsmith-pg-data:/var/lib/postgresql/data
    restart: unless-stopped

  flagsmith:
    image: flagsmith/flagsmith:latest
    container_name: flagsmith
    environment:
      DATABASE_URL: postgres://flagsmith:flagsmith_password@postgres:5432/flagsmith
      PORT: 8000
      ALLOWED_HOSTS: "localhost,127.0.0.1,flagsmith,0.0.0.0,*"
      DJANGO_ALLOWED_HOSTS: "localhost,127.0.0.1,flagsmith,0.0.0.0,*"
      FLAGSMITH_DOMAIN: "localhost:8000"
    ports:
      - "8000:8000"
    depends_on:
      - postgres
    restart: unless-stopped

  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq-slg
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
    restart: unless-stopped

volumes:
  sqlserver-data:
  flagsmith-pg-data:
  rabbitmq-data:
```

---

## ConfigurationProvider Personalizado

`FeatureManagementConfigurationProvider` consulta la API de Flagsmith periodicamente y carga los flags en `IConfiguration`:

```csharp
// FeatureToggles/FeatureManagementConfigurationProvider.cs
public class FeatureManagementConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient = new();
    private Timer? _timer;
    private readonly int _reloadIntervalSeconds;

    public override void Load()
    {
        // Carga inicial
        FetchFlags();

        // Hot reload: timer periodico
        if (_reloadIntervalSeconds > 0)
        {
            _timer = new Timer(
                callback: _ => { FetchFlags(); OnReload(); },
                state: null,
                dueTime: TimeSpan.FromSeconds(_reloadIntervalSeconds),
                period: TimeSpan.FromSeconds(_reloadIntervalSeconds));
        }
    }

    private void FetchFlags()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl);
            request.Headers.Add("X-Environment-Key", _apiKey);
            var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return;

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            var newData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var featureName = element.GetProperty("feature").GetProperty("name")
                    .GetString()?.ToUpperInvariant();
                var enabled = element.GetProperty("enabled").GetBoolean().ToString();

                newData[$"FeatureManagement:{featureName}"] = enabled;
            }

            // Reemplazar datos (no acumular)
            Data.Clear();
            foreach (var kvp in newData)
            {
                Data[kvp.Key] = kvp.Value;
            }
        }
        catch
        {
            // Si Flagsmith no esta disponible, se mantienen los valores anteriores
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _httpClient.Dispose();
    }
}
```

**Puntos clave:**

- `Load()` ejecuta `FetchFlags()` una vez al arrancar y luego arranca el `Timer`
- `FetchFlags()` reemplaza todo el diccionario `Data` (no acumula)
- `OnReload()` notifica a `IConfiguration` que hay cambios → `IFeatureManager` ve nuevos valores
- Si Flagsmith no esta disponible, el `catch` silencia el error y se mantienen los valores anteriores
- La clave es `FeatureManagement:{featureName}` que es el formato que `Microsoft.FeatureManagement` espera

> **¿Por qué un ConfigurationProvider personalizado?** Porque Flagsmith es un servicio externo que puede estar caído. Si usáramos un ConfigurationProvider normal, la app fallaría al arrancar si Flagsmith no responde. Con nuestro provider, el `catch` silencia el error y usa los valores de `appsettings` como fallback. Es un patrón de "degradación graceful": la app siempre arranca, incluso si el servicio externo no está disponible.

### Extension para Registrar

Cuando un `IConfigurationSource` devuelve un `ConfigurationProvider` personalizado, se integra en el pipeline de `IConfiguration`. El patrón separa la fuente (`IConfigurationSource`) del proveedor (`ConfigurationProvider`) mediante un método auxiliar privado:

```csharp
// Extensions/ConfigurationBuilderExtensions.cs
using SoftwareLearningGuide.Api.FeatureToggles;
using SoftwareLearningGuide.Api.Options;

namespace SoftwareLearningGuide.Api.Extensions {
    public static class ConfigurationBuilderExtensions {
        public static IConfigurationBuilder AddFeatureManagementConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration configuration) {
            AddConfigurationFromFlagApi(configurationBuilder, configuration);

            return configurationBuilder;
        }

        private static void AddConfigurationFromFlagApi(IConfigurationBuilder configurationBuilder, IConfiguration configuration) {
            var flagsApi = configuration
                            .GetSection(FeatureManagementApiConfigurationOptions.SectionName)
                            .Get<FeatureManagementApiConfigurationOptions>() ?? new FeatureManagementApiConfigurationOptions();

            if (!string.IsNullOrEmpty(flagsApi.ApiUrl))
                configurationBuilder.Add(new FeatureManagementSourceConfiguration(flagsApi.ApiUrl, flagsApi.ApiKey, flagsApi.ReloadIntervalSeconds));

        }
    }
}
```

---

## Seccion FeatureManagement en appsettings

### appsettings.json (valores por defecto)

```json
{
  "FeatureManagement": {
    "FT_APPLY_MIGRATIONS_ON_START": false,
    "FT_ENABLE_ORDER_CONTROLLER": false,
    "FT_ENABLE_ORDER_CREATION": false,
    "FT_ENABLE_ORDER_RETRIEVAL": false,
    "FT_ENABLE_ORDER_LIST": false,
    "FT_ENABLE_PRODUCT_CONTROLLER": false,
    "FT_ENABLE_PRODUCT_CREATION": false,
    "FT_ENABLE_PRODUCT_RETRIEVAL": false,
    "FT_ENABLE_PRODUCT_LIST": false,
    "FT_ENABLE_CUSTOMER_CONTROLLER": false,
    "FT_ENABLE_CUSTOMER_CREATION": false,
    "FT_ENABLE_CUSTOMER_RETRIEVAL": false,
    "FT_ENABLE_CUSTOMER_LIST": false
  },
  "FeatureManagementApiConfiguration": {
    "ApiUrl": "",
    "ApiKey": "",
    "ReloadIntervalSeconds": 0
  }
}
```

> **Observa que los valores por defecto están en `false` (feature deshabilitado).** Esto es una práctica de seguridad: si Flagsmith no está disponible, la app arranca con las features deshabilitadas, no habilitadas. Es mejor que algo no funcione a que funcione de forma no controlada. Imagina que `FT_ENABLE_ORDER_CREATION` estuviera en `true` por defecto y Flagsmith se cae: la funcionalidad de crear órdenes se habilitaría sin control, incluso en producción.

### appsettings.Development.json (valores de desarrollo)

```json
{
  "FeatureManagement": {
    "FT_APPLY_MIGRATIONS_ON_START": true,
    "FT_ENABLE_ORDER_CONTROLLER": true,
    "FT_ENABLE_ORDER_CREATION": true,
    "FT_ENABLE_ORDER_RETRIEVAL": true,
    "FT_ENABLE_ORDER_LIST": true,
    "FT_ENABLE_PRODUCT_CONTROLLER": true,
    "FT_ENABLE_PRODUCT_CREATION": true,
    "FT_ENABLE_PRODUCT_RETRIEVAL": true,
    "FT_ENABLE_PRODUCT_LIST": true,
    "FT_ENABLE_CUSTOMER_CONTROLLER": true,
    "FT_ENABLE_CUSTOMER_CREATION": true,
    "FT_ENABLE_CUSTOMER_RETRIEVAL": true,
    "FT_ENABLE_CUSTOMER_LIST": true
  },
  "FeatureManagementApiConfiguration": {
    "ApiUrl": "http://localhost:8000/api/v1/flags/",
    "ApiKey": "6xGqbGoN5rfSkAg59vnvr2",
    "ReloadIntervalSeconds": 3
  }
}
```

> **Observa que los valores de Development están en `true` (feature habilitado).** En desarrollo quieres tener todas las funcionalidades activas para probar sin restricciones. La diferencia entre appsettings.json y appsettings.Development.json es la clave: el primero es el "fallback seguro" y el segundo es el "ambiente de pruebas libre". Cuando部署s a producción, solo necesitas asegurarte de que `appsettings.Production.json` tenga los valores correctos (o que Flagsmith los gestione).

**Notas:**

- `FeatureManagementApiConfiguration:ApiUrl` vacio significa que no se consulta Flagsmith
- `ReloadIntervalSeconds: 0` desactiva el hot reload (solo carga al arrancar)
- `ReloadIntervalSeconds: 3` en Development para cambios rapidos durante desarrollo
- Los valores de `FeatureManagement` sirven como fallback cuando Flagsmith no esta disponible

---

## Uso con IFeatureManager

`Microsoft.FeatureManagement` registra `IFeatureManager` y `IFeatureManagerSnapshot` en el DI container. El proveedor de configuración personalizado se registra antes:

```csharp
// Program.cs
// 1. Registrar el provider de Flagsmith en IConfiguration
builder.Configuration.AddFeatureManagementConfiguration(builder.Configuration);
// 2. Registrar IFeatureManager e IFeatureManagerSnapshot en DI
builder.Services.AddFeatureManagement();
```

### Uso Sincrono

```csharp
var featureManager = app.Services.GetRequiredService<IFeatureManager>();

if (featureManager.IsEnabledAsync(FeatureToggleNames.FT_APPLY_MIGRATIONS_ON_START)
    .GetAwaiter().GetResult())
{
    // Aplicar migraciones
}
```

### Uso con [FeatureGate] (recomendado)

```csharp
// A nivel controller - si el flag esta off, todo el controller retorna 404
[ApiController]
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CONTROLLER)]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrderController : ControllerBase
{
    // A nivel action - solo este metodo se ve afectado
    [HttpPost]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CREATION)]
    public async Task<IActionResult> Create(...) { }
}
```

### Uso con IFeatureManagerSnapshot (en controllers)

```csharp
// Microsoft.FeatureManagement inyecta IFeatureManagerSnapshot automaticamente
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IFeatureManagerSnapshot _featureManager;

    public TestController(IFeatureManagerSnapshot featureManager)
    {
        _featureManager = featureManager;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (await _featureManager.IsEnabledAsync("FT_ENABLE_ORDER_CREATION"))
        {
            return Ok("Creacion de orden ACTIVA.");
        }

        return NotFound("Creacion de orden DESACTIVADA.");
    }
}
```

> **¿Por qué usamos `IFeatureManagerSnapshot` en controllers y no `IFeatureManager`?** Porque `IFeatureManagerSnapshot` cachea el valor del flag al inicio del request. Esto garantiza que durante todo el procesamiento del request, el valor del flag sea consistente, incluso si Flagsmith lo cambia mientras se procesa. Imagina que un request está procesando una orden y Flagsmith cambia `FT_ENABLE_ORDER_CREATION` de `true` a `false` a mitad del request: sin snapshot, el comportamiento sería inconsistente dentro del mismo request.

Microsoft.FeatureManagement soporta filtros como porcentaje de usuarios, grupos, etc.:

```json
{
  "FeatureManagement": {
    "NuevoModulo": {
      "Enabled": true,
      "Parameters": {
        "Percentage": 25,
        "Users": ["user1@example.com", "user2@example.com"]
      }
    }
  }
}
```

---

## Constantes - FeatureToggleNames

Para evitar strings magicos, se define una clase con constantes en [`SoftwareLearningGuide.Api/FeatureToggles/FeatureToggles.cs`](SoftwareLearningGuide.Api/FeatureToggles/FeatureToggles.cs). Cada controller tiene sus propias flags a nivel controller y a nivel action:

```csharp
// FeatureToggles/FeatureToggles.cs
public static class FeatureToggleNames
{
    // Migraciones
    public const string FT_APPLY_MIGRATIONS_ON_START = nameof(FT_APPLY_MIGRATIONS_ON_START);

    // Orders
    public const string FT_ENABLE_ORDER_CONTROLLER = nameof(FT_ENABLE_ORDER_CONTROLLER);
    public const string FT_ENABLE_ORDER_CREATION = nameof(FT_ENABLE_ORDER_CREATION);
    public const string FT_ENABLE_ORDER_RETRIEVAL = nameof(FT_ENABLE_ORDER_RETRIEVAL);
    public const string FT_ENABLE_ORDER_LIST = nameof(FT_ENABLE_ORDER_LIST);

    // Products
    public const string FT_ENABLE_PRODUCT_CONTROLLER = nameof(FT_ENABLE_PRODUCT_CONTROLLER);
    public const string FT_ENABLE_PRODUCT_CREATION = nameof(FT_ENABLE_PRODUCT_CREATION);
    public const string FT_ENABLE_PRODUCT_RETRIEVAL = nameof(FT_ENABLE_PRODUCT_RETRIEVAL);
    public const string FT_ENABLE_PRODUCT_LIST = nameof(FT_ENABLE_PRODUCT_LIST);

    // Customers
    public const string FT_ENABLE_CUSTOMER_CONTROLLER = nameof(FT_ENABLE_CUSTOMER_CONTROLLER);
    public const string FT_ENABLE_CUSTOMER_CREATION = nameof(FT_ENABLE_CUSTOMER_CREATION);
    public const string FT_ENABLE_CUSTOMER_RETRIEVAL = nameof(FT_ENABLE_CUSTOMER_RETRIEVAL);
    public const string FT_ENABLE_CUSTOMER_LIST = nameof(FT_ENABLE_CUSTOMER_LIST);
}
```

**Total: 13 constantes** distribuidas en:

| Categoria | Constantes | Nivel |
|-----------|------------|-------|
| **Migraciones** | `FT_APPLY_MIGRATIONS_ON_START` | Global |
| **Orders** | `FT_ENABLE_ORDER_CONTROLLER`, `FT_ENABLE_ORDER_CREATION`, `FT_ENABLE_ORDER_RETRIEVAL`, `FT_ENABLE_ORDER_LIST` | Controller + Actions |
| **Products** | `FT_ENABLE_PRODUCT_CONTROLLER`, `FT_ENABLE_PRODUCT_CREATION`, `FT_ENABLE_PRODUCT_RETRIEVAL`, `FT_ENABLE_PRODUCT_LIST` | Controller + Actions |
| **Customers** | `FT_ENABLE_CUSTOMER_CONTROLLER`, `FT_ENABLE_CUSTOMER_CREATION`, `FT_ENABLE_CUSTOMER_RETRIEVAL`, `FT_ENABLE_CUSTOMER_LIST` | Controller + Actions |

**Uso:**

```csharp
// Malo (string magico)
[FeatureGate("FT_ENABLE_ORDER_CREATION")]

// Bueno (constante)
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CREATION)]
```

---

## Diagrama de Flujo

```
App arranca
    │
    ▼
IConfiguration carga appsettings.json
    │  FeatureManagement:FT_ENABLE_ORDER = false
    │
    ▼
IConfiguration carga appsettings.Development.json
    │  FeatureManagement:FT_ENABLE_ORDER = true
    │  FeatureManagementApiConfiguration:ApiUrl = "http://localhost:8000/..."
    │  FeatureManagementApiConfiguration:ReloadIntervalSeconds = 3
    │
    ▼
ConfigurationBuilderExtensions.AddFeatureManagementConfiguration()
    │
    ├─ ApiUrl vacio? → No consulta Flagsmith, usa valores locales
    │
    └─ ApiUrl con valor? → FeatureManagementConfigurationProvider.Load()
         │
         ├─ FetchFlags() → Consulta Flagsmith, sobreescribe FeatureManagement
         │
         └─ Timer cada 3 segundos → FetchFlags() + OnReload()
              │
              ├─ Flagsmith responde OK → Actualiza valores + notifica cambios
              │
              └─ Flagsmith no responde → Mantiene valores anteriores
    │
    ▼
builder.Services.AddFeatureManagement()
    │  Registra IFeatureManager + IFeatureManagerSnapshot en DI
    │
    ▼
Request: POST /api/v1/order
    │
    ├─ [FeatureGate(FT_ENABLE_ORDER_CONTROLLER)] evalua
    │   ├─ true  → Continua
    │   └─ false → 404 Not Found
    │
    ├─ [FeatureGate(FT_ENABLE_ORDER_CREATION)] evalua
    │   ├─ true  → Ejecuta Create()
    │   └─ false → 404 Not Found
    │
    └─ Create() ejecuta logica de negocio
```

---

## Referencia de Archivos

| Archivo | Descripcion |
|---------|-------------|
| `Api/FeatureToggles/FeatureToggles.cs` | Constantes de nombres de features |
| `Api/FeatureToggles/FeatureManagementConfigurationProvider.cs` | Provider con hot reload que consulta Flagsmith |
| `Api/Extensions/ConfigurationBuilderExtensions.cs` | Extension para registrar el provider |
| `Api/Extensions/ApplicationBuilderExtensions.cs` | Extension ApplyMigrations() |
| `Api/Options/FeatureManagementApiConfigurationOptions.cs` | Options para URL, ApiKey e intervalo de reload |
| `Api/appsettings.json` | Valores por defecto de features |
| `Api/appsettings.Development.json` | Valores de desarrollo + URL de Flagsmith |
| `Api/Controllers/OrderControllerExample/OrderController.cs` | Ejemplo de [FeatureGate] a nivel controller y action |
| `docker-compose.yml` | Stack con Flagsmith + PostgreSQL |

---

## Paquetes NuGet

| Paquete | Version | Uso |
|---------|---------|-----|
| `Microsoft.FeatureManagement.AspNetCore` | 4.6.0 | Feature flags + atributo [FeatureGate] |
| `Microsoft.FeatureManagement` | 4.6.0 | Motor de feature management + IFeatureManager |
