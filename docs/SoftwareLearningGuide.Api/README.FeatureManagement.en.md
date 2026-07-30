# Feature Management - Feature Toggles and Feature Flags

![FeatureManagement](https://img.shields.io/badge/Library-Microsoft.FeatureManagement-blue)
![Flagsmith](https://img.shields.io/badge/Service-Flagsmith-green)

**Feature Management** allows controlling feature execution at runtime without redeploying the application. In this project we use **Microsoft.FeatureManagement** integrated with **Flagsmith** (external service in Docker) to manage toggles centrally with **hot reload** support.

---

#### Table of Contents

1. [Feature Toggle vs Feature Flag](#feature-toggle-vs-feature-flag)
2. [Feature Gate - Control at Attribute Level](#feature-gate---control-at-attribute-level)
3. [Feature Hot Reload - Live Changes](#feature-hot-reload---live-changes)
4. [Architecture in This Project](#architecture-in-this-project)
5. [Docker - Flagsmith](#docker---flagsmith)
6. [Custom ConfigurationProvider](#custom-configurationprovider)
7. [FeatureManagement Section in appsettings](#featuremanagement-section-in-appsettings)
8. [Usage with IFeatureManager](#usage-with-ifeaturemanager)
9. [Constants - FeatureToggleNames](#constants---featuretogglenames)
10. [Flow Diagram](#flow-diagram)
11. [File Reference](#file-reference)
12. [NuGet Packages](#nuget-packages)

---

## Feature Toggle vs Feature Flag

Although the terms are used interchangeably in the industry, they have important conceptual differences:

### Feature Toggle

A **Feature Toggle** is a mechanism that allows **enabling or disabling features** at runtime. It is used primarily for:

| Usage | Description | Example |
|-----|-------------|---------|
| **Release Toggle** | Gradually release features to users | New payment button visible only to 10% of users |
| **Experiment Toggle** | Run A/B experiments | Show layout A or layout B to measure conversion |
| **Ops Toggle** | Control operational behavior | Enable/disable cache, circuit breakers |
| **Permission Toggle** | Control access by role or market | Feature available only in EU |

**Philosophy:** The feature toggle is tied to the **feature lifecycle**. It is created, tested, and gradually released.

```csharp
// Example: Release Toggle - gradually release new functionality
if (await featureManager.IsEnabledAsync("FT_ENABLE_ORDER_CREATION"))
{
    // New logic that only executes if the toggle is active
}
```

### Feature Flag

A **Feature Flag** enables **parallel changes** safely, without releasing. It is used for:

| Usage | Description | Example |
|-----|-------------|---------|
| **Migration Flag** | Migrate between two implementations without downtime | Old database to new, without service interruption |
| **Deployment Flag** | Deploy code without activating it | Code deployed to production but disabled |
| **Kill Switch** | Quickly disable a problematic feature | If the payment service fails, disable payments immediately |
| **Permission Flag** | Control access by environment or configuration | Feature available in Development but not in Production |

**Philosophy:** The feature flag is tied to **deployment and operations**. It allows deploying code safely, migrating between implementations, maintaining system stability, and eventually removing the flag (keeping the code enabled).

```csharp
// Example: Migration Flag - migrate between two implementations
if (await featureManager.IsEnabledAsync("FT_USE_NEW_PAYMENT_GATEWAY"))
{
    return await _newPaymentGateway.ProcessAsync(order);
}
else
{
    return await _legacyPaymentGateway.ProcessAsync(order);
}
```

### Visual Summary

```mermaid
graph TD
    subgraph FT["Feature Toggle"]
        direction TD
        FT1["Releasing features"]
        FT1A["Gradual release to users"]
        FT1B["A/B experiments"]
        FT1C["Operational control"]
        FT1D["Control by role/market"]
        FT1 --> FT1A
        FT1 --> FT1B
        FT1 --> FT1C
        FT1 --> FT1D
        FT_L["Linked to: Feature lifecycle"]
    end

    subgraph FF["Feature Flag"]
        direction TD
        FF1["Parallel changes / Migrations"]
        FF1A["Migration without downtime"]
        FF1B["Safe deployment"]
        FF1C["Kill switch"]
        FF1D["Control by environment"]
        FF1 --> FF1A
        FF1 --> FF1B
        FF1 --> FF1C
        FF1 --> FF1D
        FF_L["Linked to: Deployment and operations"]
    end

    style FT fill:#e1f5fe,stroke:#0288d1
    style FF fill:#e8f5e9,stroke:#388e3c
```

> **The fundamental difference is the purpose:** a Toggle releases features gradually (like a faucet you open little by little), while a Flag enables safe migrations (like a switch you move from one wall to another). In practice, many uses overlap, but understanding the difference helps you choose the right tool.

---

## Feature Gate - Control at Attribute Level

`Microsoft.FeatureManagement.AspNetCore` provides the `[FeatureGate]` attribute that allows enabling or disabling **controller actions** or **entire controllers** directly with an attribute, without needing to write `if/else` in code.

### How It Works

When a request reaches an endpoint, the Feature Management middleware evaluates the `[FeatureGate]` attribute:
- If the feature is **enabled** → executes the action normally
- If the feature is **disabled** → returns `404 Not Found` (by default)

### Example in This Project

```csharp
// Controllers/OrderControllerExample/OrderController.cs

[ApiController]
[ApiVersion("1.0")]
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CONTROLLER)]  // ← Controls the entire controller
[Route("api/v{version:apiVersion}/[controller]")]
public class OrderController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_RETRIEVAL)]  // ← Controls only this action
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        // ...
    }

    [HttpPost]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CREATION)]  // ← Controls only this action
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // ...
    }
}
```

### Control Levels

| Level | Attribute | Effect |
|-------|----------|--------|
| **Controller** | `[FeatureGate("FT_X")]` on the class | If FT_X is off, the ENTIRE controller returns 404 |
| **Action** | `[FeatureGate("FT_X")]` on the method | If FT_X is off, only that method returns 404 |

### Flow with FeatureGate

```mermaid
graph TD
    A["Request: GET /api/v1/order/{id}"] --> B["[FeatureGate(FT_ENABLE_ORDER_RETRIEVAL)] is evaluated"]
    B --> C{"FT_ENABLE_ORDER_RETRIEVAL = ?"}
    C -- "true" --> D["Executes GetById()"]
    C -- "false" --> E["Returns 404 Not Found"]

    style A fill:#e3f2fd,stroke:#1565c0
    style D fill:#e8f5e9,stroke:#2e7d32
    style E fill:#ffebee,stroke:#c62828
```

### Multiple Usage (AND/OR)

```csharp
// AND: Both features must be active
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CONTROLLER, FeatureToggleNames.FT_ENABLE_ORDER_CREATION)]
public async Task<IActionResult> Create(...) { }

// OR: At least one must be active (using FeatureGateFilter)
```

---

## Feature Hot Reload - Live Changes

**Feature Hot Reload** allows feature flags to update in real time without restarting the application. When you change a flag in Flagsmith, the application reflects it automatically within seconds.

### How It Works

```mermaid
sequenceDiagram
    participant WP as Flagsmith Web Panel
    participant F as Flagsmith (Docker)
    participant App as .NET Application

    Note over App: Load() + Timer every N seconds

    WP->>WP: You change a flag in the web panel

    App->>F: FetchFlags() (Periodic polling)
    F-->>App: Returns updated flags

    App->>App: OnReload() - Notifies IConfiguration

    Note over App: IFeatureManager reads new values

    WP->>App: GET /api/v1/order
    App-->>WP: Response with new behavior
    Note over App: Feature evaluated with updated value
```

### Configuration

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

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `ReloadIntervalSeconds` | How often to query Flagsmith | `30` |
| `ReloadIntervalSeconds = 0` | Disables polling (only loads on startup) | - |

### Practical Example

```
1. App starts with FT_ENABLE_ORDER_CREATION = true
2. You create an order → works OK
3. In Flagsmith, you disable FT_ENABLE_ORDER_CREATION
4. You wait 3 seconds (ReloadIntervalSeconds)
5. You try to create another order → 404 Not Found (without restarting the app)
```

### IFeatureManagerSnapshot vs IFeatureManager

```csharp
// IFeatureManager: reads fresh values on each call (recommended for hot reload)
private readonly IFeatureManager _featureManager;

// IFeatureManagerSnapshot: caches the value per request (faster but doesn't see instant changes)
private readonly IFeatureManagerSnapshot _featureManager;
```

`Microsoft.FeatureManagement` automatically injects `IFeatureManagerSnapshot` into controllers, which captures the flag value at the start of the request. This ensures consistency within a single request.

---

## Architecture in This Project

The implementation uses a **dual-source** architecture with priority and **hot reload**:

```mermaid
graph TD
    FW[External source<br/>Flagsmith in Docker] -->|"HIGH PRIORITY<br/>(overwrites)"| IC[Microsoft IConfiguration]

    subgraph IC [Microsoft IConfiguration]
        ICF[FeatureManagement:FT_ENABLE_ORDER_CREATION = true<br/><i>Flagsmith (external)</i>]
        ICA[FeatureManagement:FT_ENABLE_ORDER_CREATION = false<br/><i>appsettings (internal)</i>]
        ICR[The last registered provider wins.<br/>Flagsmith is registered AFTER appsettings]
        ICH[Timer every N seconds → FetchFlags() → OnReload()<br/><i>Hot Reload</i>]
    end

    style FW fill:#e1f5fe,stroke:#01579b
    style IC fill:#fff3e0,stroke:#e65100
```

**Loading flow:**

1. `appsettings.json` loads default values (FeatureManagement section)
2. `appsettings.Development.json` overrides with Development values
3. `FeatureManagementConfigurationProvider` queries Flagsmith and overrides whatever it finds
4. Every `ReloadIntervalSeconds`, the provider re-queries Flagsmith and notifies changes via `OnReload()`

If Flagsmith is not available or the URL is empty, the appsettings values are used.

---

## Docker - Flagsmith

The Docker stack includes **Flagsmith** as a feature flag management service with PostgreSQL as the backend.

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

### Services

| Service | URL | Port | Description |
|----------|-----|--------|-------------|
| **Flagsmith** | http://localhost:8000 | 8000 | Administration panel and API |
| **PostgreSQL** | localhost:5432 | 5432 | Flagsmith database |

### Complete Docker Stack

The complete `docker-compose.yml` includes additional services for SQL Server, RabbitMQ, and Aspire Dashboard:

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

## Custom ConfigurationProvider

`FeatureManagementConfigurationProvider` queries the Flagsmith API periodically and loads flags into `IConfiguration`:

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
        // Initial load
        FetchFlags();

        // Hot reload: periodic timer
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

            // Replace data (don't accumulate)
            Data.Clear();
            foreach (var kvp in newData)
            {
                Data[kvp.Key] = kvp.Value;
            }
        }
        catch
        {
            // If Flagsmith is not available, previous values are kept
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _httpClient.Dispose();
    }
}
```

**Key points:**

- `Load()` executes `FetchFlags()` once at startup and then starts the `Timer`
- `FetchFlags()` replaces the entire `Data` dictionary (doesn't accumulate)
- `OnReload()` notifies `IConfiguration` of changes → `IFeatureManager` sees new values
- If Flagsmith is not available, the `catch` silences the error and keeps previous values
- The key is `FeatureManagement:{featureName}` which is the format `Microsoft.FeatureManagement` expects

> **Why a custom ConfigurationProvider?** Because Flagsmith is an external service that may be down. If we used a regular ConfigurationProvider, the app would fail to start if Flagsmith doesn't respond. With our provider, the `catch` silences the error and uses the `appsettings` values as a fallback. It's a "graceful degradation" pattern: the app always starts, even if the external service is unavailable.

### Extension for Registration

When an `IConfigurationSource` returns a custom `ConfigurationProvider`, it integrates into the `IConfiguration` pipeline. The pattern separates the source (`IConfigurationSource`) from the provider (`ConfigurationProvider`) using a private helper method:

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

## FeatureManagement Section in appsettings

### appsettings.json (default values)

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

> **Notice the default values are `false` (feature disabled).** This is a security practice: if Flagsmith is unavailable, the app starts with features disabled, not enabled. It's better for something not to work than to work in an uncontrolled way. Imagine if `FT_ENABLE_ORDER_CREATION` defaulted to `true` and Flagsmith went down: the order creation functionality would be enabled without control, even in production.

### appsettings.Development.json (development values)

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

> **Notice the Development values are `true` (feature enabled).** In development you want all features active to test without restrictions. The difference between appsettings.json and appsettings.Development.json is the key: the former is the "safe fallback" and the latter is the "unrestricted testing environment". When deploying to production, you just need to ensure `appsettings.Production.json` has the correct values (or that Flagsmith manages them).

**Notes:**

- `FeatureManagementApiConfiguration:ApiUrl` empty means Flagsmith is not queried
- `ReloadIntervalSeconds: 0` disables hot reload (only loads on startup)
- `ReloadIntervalSeconds: 3` in Development for rapid changes during development
- `FeatureManagement` values serve as a fallback when Flagsmith is unavailable

---

## Usage with IFeatureManager

`Microsoft.FeatureManagement` registers `IFeatureManager` and `IFeatureManagerSnapshot` in the DI container. The custom configuration provider is registered first:

```csharp
// Program.cs
// 1. Register the Flagsmith provider in IConfiguration
builder.Configuration.AddFeatureManagementConfiguration(builder.Configuration);
// 2. Register IFeatureManager and IFeatureManagerSnapshot in DI
builder.Services.AddFeatureManagement();
```

### Synchronous Usage

```csharp
var featureManager = app.Services.GetRequiredService<IFeatureManager>();

if (featureManager.IsEnabledAsync(FeatureToggleNames.FT_APPLY_MIGRATIONS_ON_START)
    .GetAwaiter().GetResult())
{
    // Apply migrations
}
```

### Usage with [FeatureGate] (recommended)

```csharp
// At controller level - if the flag is off, the entire controller returns 404
[ApiController]
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CONTROLLER)]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrderController : ControllerBase
{
    // At action level - only this method is affected
    [HttpPost]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CREATION)]
    public async Task<IActionResult> Create(...) { }
}
```

### Usage with IFeatureManagerSnapshot (in controllers)

```csharp
// Microsoft.FeatureManagement automatically injects IFeatureManagerSnapshot
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
            return Ok("Order creation ACTIVE.");
        }

        return NotFound("Order creation DISABLED.");
    }
}
```

> **Why do we use `IFeatureManagerSnapshot` in controllers and not `IFeatureManager`?** Because `IFeatureManagerSnapshot` caches the flag value at the start of the request. This ensures that throughout the request processing, the flag value remains consistent, even if Flagsmith changes it while processing. Imagine a request is processing an order and Flagsmith changes `FT_ENABLE_ORDER_CREATION` from `true` to `false` mid-request: without a snapshot, the behavior would be inconsistent within the same request.

Microsoft.FeatureManagement supports filters like user percentages, groups, etc.:

```json
{
  "FeatureManagement": {
    "NewModule": {
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

## Constants - FeatureToggleNames

To avoid magic strings, a class with constants is defined in [`SoftwareLearningGuide.Api/FeatureToggles/FeatureToggles.cs`](SoftwareLearningGuide.Api/FeatureToggles/FeatureToggles.cs). Each controller has its own flags at controller and action levels:

```csharp
// FeatureToggles/FeatureToggles.cs
public static class FeatureToggleNames
{
    // Migrations
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

**Total: 13 constants** distributed as:

| Category | Constants | Level |
|-----------|------------|-------|
| **Migrations** | `FT_APPLY_MIGRATIONS_ON_START` | Global |
| **Orders** | `FT_ENABLE_ORDER_CONTROLLER`, `FT_ENABLE_ORDER_CREATION`, `FT_ENABLE_ORDER_RETRIEVAL`, `FT_ENABLE_ORDER_LIST` | Controller + Actions |
| **Products** | `FT_ENABLE_PRODUCT_CONTROLLER`, `FT_ENABLE_PRODUCT_CREATION`, `FT_ENABLE_PRODUCT_RETRIEVAL`, `FT_ENABLE_PRODUCT_LIST` | Controller + Actions |
| **Customers** | `FT_ENABLE_CUSTOMER_CONTROLLER`, `FT_ENABLE_CUSTOMER_CREATION`, `FT_ENABLE_CUSTOMER_RETRIEVAL`, `FT_ENABLE_CUSTOMER_LIST` | Controller + Actions |

**Usage:**

```csharp
// Bad (magic string)
[FeatureGate("FT_ENABLE_ORDER_CREATION")]

// Good (constant)
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CREATION)]
```

---

## Flow Diagram

```mermaid
graph TD
    A["App starts"] --> B["IConfiguration loads appsettings.json<br/>FeatureManagement:FT_ENABLE_ORDER = false"]
    B --> C["IConfiguration loads appsettings.Development.json<br/>FeatureManagement:FT_ENABLE_ORDER = true<br/>ApiUrl = localhost:8000/...<br/>ReloadIntervalSeconds = 3"]
    C --> D["ConfigurationBuilderExtensions<br/>.AddFeatureManagementConfiguration()"]
    D --> E{"ApiUrl empty?"}
    E -- "Yes" --> F["Doesn't query Flagsmith<br/>uses local values"]
    E -- "No" --> G["FeatureManagementConfigurationProvider<br/>.Load()"]
    G --> H["FetchFlags()<br/>Queries Flagsmith, overrides FeatureManagement"]
    G --> I["Timer every 3 seconds<br/>FetchFlags() + OnReload()"]
    I --> J{"Flagsmith responds?"}
    J -- "OK" --> K["Updates values<br/>notifies changes"]
    J -- "No response" --> L["Keeps previous values"]
    H --> M["builder.Services.AddFeatureManagement()<br/>Registers IFeatureManager +<br/>IFeatureManagerSnapshot in DI"]
    K --> M
    L --> M
    F --> M
    M --> N["Request: POST /api/v1/order"]
    N --> O{"[FeatureGate(FT_ENABLE_ORDER_CONTROLLER)]"}
    O -- "true" --> P{"[FeatureGate(FT_ENABLE_ORDER_CREATION)]"}
    O -- "false" --> Q["404 Not Found"]
    P -- "true" --> R["Create() executes business logic"]
    P -- "false" --> Q

    style A fill:#e3f2fd,stroke:#1565c0
    style F fill:#fff3e0,stroke:#e65100
    style Q fill:#ffebee,stroke:#c62828
    style R fill:#e8f5e9,stroke:#2e7d32
```

---

## File Reference

| File | Description |
|---------|-------------|
| `Api/FeatureToggles/FeatureToggles.cs` | Feature name constants |
| `Api/FeatureToggles/FeatureManagementConfigurationProvider.cs` | Provider with hot reload that queries Flagsmith |
| `Api/Extensions/ConfigurationBuilderExtensions.cs` | Extension to register the provider |
| `Api/Extensions/ApplicationBuilderExtensions.cs` | Extension ApplyMigrations() |
| `Api/Options/FeatureManagementApiConfigurationOptions.cs` | Options for URL, ApiKey and reload interval |
| `Api/appsettings.json` | Default feature values |
| `Api/appsettings.Development.json` | Development values + Flagsmith URL |
| `Api/Controllers/OrderControllerExample/OrderController.cs` | Example of [FeatureGate] at controller and action level |
| `docker-compose.yml` | Stack with Flagsmith + PostgreSQL |

---

## NuGet Packages

| Package | Version | Usage |
|---------|---------|-------|
| `Microsoft.FeatureManagement.AspNetCore` | 4.6.0 | Feature flags + [FeatureGate] attribute |
| `Microsoft.FeatureManagement` | 4.6.0 | Feature management engine + IFeatureManager |
