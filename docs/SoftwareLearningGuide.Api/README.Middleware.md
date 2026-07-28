# Middleware - Global Exception Handling

![Middleware](https://img.shields.io/badge/Pattern-Global_Exception_Handling-red)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Pipeline-orange)

Un **middleware** global captura cualquier excepción no controlada que se eleve en el pipeline HTTP, la loguea con contexto completo y devuelve una respuesta 500 al cliente. Evita que errores inesperados lleguen al usuario con HTML de error por defecto.

---

## Tabla de Contenidos

1. [¿Qué es un Middleware?](#qué-es-un-middleware)
2. [GlobalExceptionMiddleware](#globalexceptionmiddleware)
3. [Scopes de Logging en el Middleware](#scopes-de-logging-en-el-middleware)
4. [Respuesta del Error](#respuesta-del-error)
5. [Posición en el Pipeline](#posición-en-el-pipeline)
6. [Flujo de una Excepción](#flujo-de-una-excepción)
7. [¿Por qué Scopes en el Middleware?](#por-qué-scopes-en-el-middleware)
8. [Referencia de Archivos](#referencia-de-archivos)

---

## ¿Qué es un Middleware?

Un **middleware** es una clase que procesa cada petición HTTP antes o después de que llegue al siguiente componente del pipeline. Cada middleware puede:

- Ejecutar código antes del siguiente middleware
- Modificar la petición o respuesta
- **Cortocircuitar** el pipeline (no llamar al siguiente)
- Manejar errores

```
Request → [Middleware 1] → [Middleware 2] → [Controller] → Response
```

### Analogía

Piensa en el middleware como una **cadena de seguridad** en una fábrica:

```
Entrada de materiales
    │
    ▼
[Inspección de calidad]  ← middleware
    │
    ▼
[Procesamiento]          ← controller
    │
    ▼
[Empaquetado]            ← otro middleware
    │
    ▼
Salida de producto
```

Si algo falla en cualquier paso, la cadena de seguridad lo atrapa y reporta el problema sin detener toda la fábrica.

Sin un middleware de excepciones global, cada controller tendría que manejar sus propias excepciones con `try-catch`. Esto genera código duplicado y, peor aún, si un controller se olvida de capturar una excepción, el usuario ve una página de error HTML fea. El middleware centraliza el manejo de errores en un solo lugar: un único punto de control que atrapa **cualquier** excepción no controlada en toda la aplicación.

---

## GlobalExceptionMiddleware

El middleware intercepta cualquier excepción no controlada que se produzca después de que una petición entra en el pipeline y antes de que la respuesta se envíe al cliente. Lo consigue ejecutando el resto de la pipeline dentro de un bloque `try/catch` envuelto en un scope de logging que persiste durante toda la duración de la petición, lo que garantiza que incluso si el scope de un controller se elimina durante el propagado de la excepción, el log de error conservará los datos de contexto (CorrelationId, Method, Path, IP).

[`SoftwareLearningGuide.Api/Middlewares/GlobalExceptionMiddleware.cs`](SoftwareLearningGuide.Api/Middlewares/GlobalExceptionMiddleware.cs)

```csharp
public sealed class GlobalExceptionMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger) {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", context.TraceIdentifier },
            { "RequestMethod", context.Request.Method },
            { "RequestPath", context.Request.Path },
            { "RemoteIpAddress", context.Connection.RemoteIpAddress?.ToString() ?? "unknown" }
        })) {
            try {
                await _next(context);
            }
            catch (Exception ex) {
                var extraProperties = context.Items["LogProperties"] as IDictionary<string, object>;

                var scope = extraProperties is { Count: > 0 }
                    ? _logger.BeginScope(extraProperties)
                    : null;

                try {
                    _logger.LogError(ex,
                        "Excepción no controlada en {Method} {Path}{QueryString}. TraceId: {TraceId} con Mensaje: {Message}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Request.QueryString,
                        context.TraceIdentifier,
                        ex.Message);
                }
                finally {
                    scope?.Dispose();
                }

                await HandleExceptionAsync(context, ex);
            }
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception) {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new {
            error = "An unexpected error occurred.",
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
```

### Desglose del Middleware

Vamos a desglosar lo que hace este middleware paso a paso:

1. **Scope de request** (líneas 79-85): Crea un scope que envuelve toda la request con datos de contexto (CorrelationId, Method, Path, IP). Cualquier log que se escriba dentro de `_next(context)` tendrá estos campos adjuntos automáticamente.

2. **Ejecución del pipeline** (línea 89): Llama a `_next(context)` para ejecutar el siguiente middleware o controller. Si todo va bien, el scope se dispose normalmente al salir del `using`.

3. **Captura de excepciones** (líneas 92-118): Si algo falla, captura la excepción, lee las propiedades adicionales que el controller haya guardado en `HttpContext.Items`, las loguea con todo el contexto disponible, y devuelve una respuesta JSON genérica al cliente con el `traceId` para que el equipo pueda rastrear el error.

4. **Respuesta segura** (método `HandleExceptionAsync`): Devuelve un JSON genérico con `"error"` y `"traceId"`. Nunca expone detalles internos como stack traces o mensajes de error al cliente.

### Puntos Clave

| Concepto | Descripción |
|----------|-------------|
| `RequestDelegate _next` | Delegate al siguiente middleware en el pipeline |
| `ILogger _logger` | Logger inyectado por DI para loguear el error |
| **Scope de request** | Envuelve toda la request, incluido el catch |
| **Scope de controller** | Se lee de `HttpContext.Items` para enriquecer el log de error |
| `context.TraceIdentifier` | ID único de la solicitud para correlación con OpenTelemetry |
| **Respuesta JSON** | Error genérico al cliente, detalle completo en logs |

---

## Scopes de Logging en el Middleware

### ¿Qué son los Scopes?

Un **scope** es un "contexto" que se adjunta a todos los logs que se escriben dentro de él. Es como poner un sticker en cada log que dice "este log pertenece a esta request".

Sin scopes, cada log sería un evento aislado. Con scopes, cada log lleva adjunta información del contexto (como el ID de la request). Es como poner una etiqueta en cada pieza de un rompecabezas: sabes a qué request pertenece. Esto es fundamental en producción donde tienes cientos de requests concurrentes — sin scopes, sería imposible distinguir qué log pertenece a qué usuario, qué petición o qué operación.

### Scope de Request (Nivel Middleware)

El middleware crea un scope que **envuelve toda la request**. Este scope es el único que permanece activo durante el bloque `catch`, por lo que es el mecanismo principal para que los logs de error conserven contexto de correlación. La definición de los campos del scope se encuentra en [`SoftwareLearningGuide.Api/Middlewares/GlobalExceptionMiddleware.cs`](SoftwareLearningGuide.Api/Middlewares/GlobalExceptionMiddleware.cs).

[`SoftwareLearningGuide.Api/Middlewares/GlobalExceptionMiddleware.cs`](SoftwareLearningGuide.Api/Middlewares/GlobalExceptionMiddleware.cs)

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    { "CorrelationId", context.TraceIdentifier },
    { "RequestMethod", context.Request.Method },
    { "RequestPath", context.Request.Path },
    { "RemoteIpAddress", ... }
}))
{
    await _next(context);
    // TODOS los logs aquí (controllers, handlers, repositories)
    // tienen CorrelationId, RequestMethod, etc. automáticamente
}
```

### Scope de Controller (Nivel Acción)

El controller enriquece el contexto de logging con datos específicos de la acción que se está ejecutando. En la práctica, esto permite que cada log dentro de esa acción se correlacione no solo con la request (gracias al scope del middleware) sino también con la entidad o operación concreta que se está procesando. A diferencia del scope del middleware, el scope del controller se elimina al finalizar la acción o al propagarse una excepción, por lo que los logs de error solo conservan el scope del middleware. La implementación se puede ver en [`SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs`](SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs).

[`SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs`](SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs)

```csharp
// En el controller:
HttpContext.Items["CustomerId"] = command.CustomerId;
HttpContext.Items["LineCount"] = command.Lines.Count;

using (_logger.BeginScope(new Dictionary<string, object>
{
    { "CustomerId", command.CustomerId },
    { "LineCount", command.Lines.Count }
})) {
    _logger.LogInformation(
        "Iniciando creación de orden para cliente {CustomerId} con {LineCount} líneas",
        command.CustomerId, command.Lines.Count);
}
```

### Jerarquía Visual

```
Request: POST /api/v1/order
│
├── [Middleware Scope] CorrelationId=abc, Method=POST, Path=/api/v1/order, IP=127.0.0.1
│   │
│   ├── [Controller Scope] CustomerId=550e8400, LineCount=3
│   │   │
│   │   ├── Log: "Iniciando creación..."
│   │   │   → CorrelationId=abc, Method=POST, Path=/api/v1/order, IP=127.0.0.1,
│   │   │     CustomerId=550e8400, LineCount=3
│   │   │
│   │   └── EXCEPCIÓN → Controller scopeDisposed (using block termina)
│   │
│   └── catch: _logger.LogError(ex, "Excepción...")
│       → CorrelationId=abc, Method=POST, Path=/api/v1/order, IP=127.0.0.1
│       (scope del controller YA NO EXISTE, pero el del middleware SÍ)
```

### ¿Por qué el Scope del Middleware es Crítico para Excepciones?

Cuando una excepción ocurre dentro del `using` block del controller:

```
Controller: using (BeginScope({CustomerId, LineCount})) {
    _mediator.Send(command);  ← ¡EXCEPCIÓN!
}
// Dispose() se llama aquí durante stack unwinding
// El scope del controller YA NO EXISTE

Middleware: catch (Exception ex) {
    // El scope del controller fue disposed
    // PERO el scope del middleware SIGUE ACTIVO
    _logger.LogError(ex, "...");  // ← SÍ tiene CorrelationId, Method, Path
}
```

**Por eso el middleware crea su propio scope**: es el **único** que garantiza que los datos de contexto estén disponibles cuando se loguea la excepción.

---

## Respuesta del Error

El cliente recibe una respuesta **500** con JSON:

```json
{
  "error": "An unexpected error occurred.",
  "traceId": "0HN1H87G5NL2F:00000001"
}
```

### ¿Por qué un error genérico?

| Razón | Descripción |
|-------|-------------|
| **Seguridad** | No exponer detalles internos del servidor al cliente |
| **Producción** | Un `NullReferenceException` no debe llegar al usuario |
| **Logging** | El detalle completo queda en los logs del servidor |
| **Correlación** | El `traceId` permite al equipo rastrear el error en OpenTelemetry |

### Flujo del Error

```
Cliente envía request → Middleware captura excepción
                              │
                    ┌─────────┴─────────┐
                    ▼                   ▼
            Log completo         Respuesta JSON
            (con stack trace,    (genérica, sin
            correlationId,       detalles internos)
            contexto)
```

---

## Posición en el Pipeline

El middleware se registra **antes** de `UseHttpsRedirection` para capturar errores de cualquier middleware o controller posterior:

```csharp
// Program.cs
var app = builder.Build();

// ... Swagger/OpenAPI (solo Development) ...

app.UseMiddleware<GlobalExceptionMiddleware>();  // ← Aquí

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Flujo del Pipeline

```
Request entrante
    │
    ▼
┌─────────────────────────────────┐
│  GlobalExceptionMiddleware      │
│  ┌───────────────────────────┐  │
│  │  BeginScope(request)      │  │
│  │  ┌─────────────────────┐  │  │
│  │  │  try {              │  │  │
│  │  │    await _next(ctx) │──┼──┼──→ UseHttpsRedirection
│  │  │  }                  │  │  │         │
│  │  │  catch (Exception) {│  │  │         ▼
│  │  │    LogError(ex)     │  │  │     UseAuthorization
│  │  │    Return 500 JSON  │  │  │         │
│  │  │  }                  │  │  │         ▼
│  │  └─────────────────────┘  │  │     MapControllers
│  │  EndScope                 │  │         │
│  └───────────────────────────┘  │         ▼
└─────────────────────────────────┘     Controller action
                                            │
                                            ▼
                                        Response
```

---

## Flujo de una Excepción

```
1. Controller lanza una excepción (ej: StockInsuficienteException)
         │
         ▼
2. El using block del controller hace Dispose() del scope del controller
   (scope del controller desaparece del contexto de logging)
         │
         ▼
3. La excepción propaga al GlobalExceptionMiddleware
         │
         ▼
4. El middleware entra al bloque catch:
   a. Lee HttpContext.Items["LogProperties"] (propiedades adicionales del controller)
   b. Crea un scope anidado con esas propiedades (si existen)
   c. Loguea el error CON el scope del middleware activo:
      - CorrelationId: 0HN1H87G5NL2F:00000001
      - RequestMethod: POST
      - RequestPath: /api/v1/order
      - RemoteIpAddress: 127.0.0.1
      - Exception completa (stack trace)
         │
         ▼
5. Se devuelve respuesta 500 JSON al cliente:
   {"error":"An unexpected error occurred.","traceId":"0HN1H87G5NL2F:00000001"}
         │
         ▼
6. El scope del middleware se disposed (using block termina)
         │
         ▼
7. El equipo usa el traceId para buscar el error en OpenTelemetry/Grafana/Aspire
```

---

## ¿Por qué Scopes en el Middleware?

### El Problema Original

Antes de usar scopes en el middleware, el problema era:

```
Controller crea scope({ CorrelationId, CustomerId, ... })
    │
    ▼
Excepción → using block dispose el scope
    │
    ▼
Middleware catch → LogError SIN scope → El log NO tiene CorrelationId
    │
    ▼
Resultado: No puedes correlacionar el error con la request
```

### La Solución

El middleware crea su **propio scope** que vive durante toda la request:

```
Middleware crea scope({ CorrelationId, Method, Path, IP })
    │
    ▼
Controller crea scope adicional({ CustomerId, LineCount })
    │
    ▼
Excepción → scope del controller disposed, pero scope del middleware SIGUE
    │
    ▼
Middleware catch → LogError CON scope del middleware → SÍ tiene CorrelationId
```

### HttpContext.Items como Puente

`HttpContext.Items` es un diccionario que vive durante **toda la request** (a diferencia de los scopes que se disposed). El controller guarda datos ahí, y el middleware los lee en el catch:

```csharp
// Controller:
HttpContext.Items["CustomerId"] = command.CustomerId;

// Middleware (en el catch):
var extraProperties = context.Items["LogProperties"] as IDictionary<string, object>;
```

Esto permite que cualquier parte de la app enriquezca el log de error sin acoplarse al middleware.

### Diferencia clave: Scopes vs. HttpContext.Items

Los **scopes** se disposed automáticamente al salir del bloque `using` — tienen un ciclo de vida limitado. Pero `HttpContext.Items` vive durante **toda la request**, desde que llega hasta que se responde. Por eso el middleware lee de `Items` en el `catch`: el scope del controller ya no existe (se disposed al propagarse la excepción), pero `Items` sigue ahí con los datos que el controller guardó. Es el mecanismo de respaldo que garantiza que no se pierda información de contexto cuando algo falla.

---

## Referencia de Archivos

| Archivo | Descripción |
|---------|-------------|
| `SoftwareLearningGuide.Api/Middlewares/GlobalExceptionMiddleware.cs` | Middleware de excepciones globales con scopes |
| `SoftwareLearningGuide.Api/Program.cs` | Registro en el pipeline HTTP (línea 68) |
| `SoftwareLearningGuide.Api/Controllers/OrderControllerExample/OrderController.cs` | Controller que usa scopes y HttpContext.Items |

---

## Notas

- El middleware **no re-lanza** la excepción (`throw`). Si lo hiciera, el pipeline se detendría y Kestrel devolvería una respuesta HTML por defecto.
- Se registra como `UseMiddleware<T>()` porque no necesita configuración. Si necesitara opciones, se usaría `UseMiddleware<T>(options)`.
- Funciona con **cualquier excepción** no controlada, independientemente de dónde se produzca en el pipeline.
- Los scopes son **thread-safe** y se propagan correctamente en código asíncrono (`async/await`).
- El scope del middleware es el **único** que garantiza disponibilidad durante el log de excepciones.
