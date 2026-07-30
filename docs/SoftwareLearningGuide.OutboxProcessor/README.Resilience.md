# Resiliencia en la Comunicacion con RabbitMQ

![Resilience](https://img.shields.io/badge/Pattern-Resilience-blue)
![MassTransit](https://img.shields.io/badge/Messaging-MassTransit-green)
![RabbitMQ](https://img.shields.io/badge/Broker-RabbitMQ-orange)
![Polly](https://img.shields.io/badge/Policy-Polly-red)

La **resiliencia** es la capacidad de un sistema para recuperarse de fallos y seguir funcionando. En un sistema distribuido donde el `OutboxProcessor` se comunica con RabbitMQ, los fallos de red, timeouts o sobrecarga del broker son inevitables. Este documento explica como se implementan **tres patrones de resiliencia** en el pipeline de MassTransit.

---

#### Tabla de Contenidos

1. [El Problema que Resuelve](#el-problema-que-resuelve)
2. [Los Tres Patrones de Resiliencia](#los-tres-patrones-de-resiliencia)
   - [Retry (Reintentos)](#retry-reintentos)
   - [Circuit Breaker (Cortacircuitos)](#circuit-breaker-cortacircuitos)
   - [Timeout (Tiempo de Espera)](#timeout-tiempo-de-espera)
3. [Arquitectura: Pipeline de Resiliencia en MassTransit](#arquitectura-pipeline-de-resiliencia-en-masstransit)
4. [Configuracion](#configuracion)
5. [Implementacion en Codigo](#implementacion-en-codigo)
6. [Ejemplos: Que Sucede ante un Fallo](#ejemplos-que-sucede-ante-un-fallo)
7. [Estructura de Archivos](#estructura-de-archivos)
8. [Recursos Recomendados](#recursos-recomendados)

---

## El Problema que Resuelve

El `OutboxProcessor` es un Worker Service que lee mensajes de `DomainOutboxMessages` y los publica a RabbitMQ. Si RabbitMQ no esta disponible, la comunicacion falla. Sin resiliencia:

```
OutboxProcessor → Publica a RabbitMQ → FALLO (timeout, red caida, broker sobrecargado)
```

**Resultado:** El mensaje queda en la tabla outbox pero no se entrega. Si no hay reintentos ni circuit breaker, el sistema puede:

- **Quedarse colgado** esperando una respuesta que nunca llega
- **Acumular reintentos infinitos** consumiendo recursos
- **Satuar la red** con llamadas a un servicio caido

> **Piensa en la resiliencia como un fuse (fusible) electrico.** Si un circuito recibe demasiada corriente, el fusible se quema para proteger el sistema. Si RabbitMQ esta sobrecargado, el circuit breaker "se abre" y deja de enviarle trafico, dandole tiempo de recuperarse. Cuando vuelve a estar sano, el trafico fluye normalmente.

---

## Los Tres Patrones de Resiliencia

### Retry (Reintentos)

El patron **Retry** reintenta una operacion que falla antes de rendirse. En MassTransit se configura con `UseMessageRetry` y soporta varias estrategias:

| Estrategia | Descripcion | Cuando usarla |
|------------|-------------|---------------|
| `Immediate` | Reintenta inmediatamente N veces | Fallos muy transitorios (ej: conexion momentanea) |
| `Interval` | Reintenta despues de un fijo fijo | Cuando necesitas un delay constante |
| `Intervals` | Reintenta con intervalos especificos | Cuando quieres control total |
| **`Exponential`** | Backoff exponencial (1s, 2s, 4s, 8s...) | **La mas comun: balance entre rapidez y carga** |
| `Incremental` | Incremento lineal (1s, 2s, 3s...) | Cuando el backoff exponencial es demasiado agresivo |

> **¿Por que usamos Exponential?** Porque es la estrategia mas equilibrada. Si RabbitMQ se cae por 30 segundos, un retry inmediato (Immediate) generaria cientos de llamadas fallidas en ese tiempo. Un backoff exponencial empieza rapido pero se espacia, reduciendo la carga sobre el broker caido mientras llega rapido cuando se recupera.

#### Ejemplo Visual: Exponential Backoff

```
Intento 1:  0s     → FALLO
Intento 2:  1s     → FALLO
Intento 3:  6s     → FALLO (1 + 5 delta)
Intento 4:  11s    → FALLO (6 + 5 delta)
Intento 5:  16s    → FALLO (11 + 5 delta)
Intento 6:  21s    → OK (RabbitMQ se recupero)

Maximo: 30 segundos entre reintentos
```

### Circuit Breaker (Cortacircuitos)

El patron **Circuit Breaker** monitorea los fallos y, cuando alcanzan un umbral, "abre el circuito" y rechaza llamadas inmediatamente sin intentar la operacion. Esto protege tanto al cliente como al servicio remoto.

#### Estados del Circuit Breaker

```
                    ┌─────────────────┐
                    │                 │
    OK ────────────►│   CLOSED        │───── Funciona normalmente
    │               │   (Normal)      │
    │               └────────┬────────┘
    │                        │
    │                        │ Fallos >= TripThreshold
    │                        ▼
    │               ┌─────────────────┐
    │               │                 │
    │               │   OPEN          │───── Rechaza llamadas inmediatamente
    │               │   (Proteccion)  │      (excepcion rapida)
    │               └────────┬────────┘
    │                        │
    │                        │ ResetInterval expira
    │                        ▼
    │               ┌─────────────────┐
    │               │                 │
    │               │   HALF-OPEN     │───── Prueba con una llamada
    │               │   (Prueba)      │
    │               └────────┬────────┘
    │                        │
    │                        ├── OK → CLOSED (vuelve a normal)
    │                        └── FALLO → OPEN (sigue protegiendo)
    │
    └────────────────────────┘
```

#### Propiedades Configurables

| Propiedad | Tipo | Descripcion |
|-----------|------|-------------|
| `TripThreshold` | `int` | Numero de fallos en el TrackingPeriod para abrir el circuito |
| `ActiveThreshold` | `int` | Numero de llamadas activas minimo antes de evaluar el threshold |
| `TrackingPeriodMinutes` | `int` | Ventana de tiempo en minutos para contar fallos |
| `ResetIntervalMinutes` | `int` | Tiempo en minutos antes de intentar pasar a HALF-OPEN |

> **¿Por que un circuit breaker con RabbitMQ?** Si RabbitMQ se cae y el OutboxProcessor sigue intentando publicar, cada reintento consume memoria, CPU y tiempo de espera. Con 20 mensajes en la outbox y 3 reintentos cada uno, serian 60 llamadas fallidas. El circuit breaker detecta el patron de fallos y dice "basta, no mas llamadas hasta que RabbitMQ se recupere". Cuando el `ResetInterval` expira, hace una sola prueba. Si funciona, vuelve a cerrar el circuito.

### Timeout (Tiempo de Espera)

El patron **Timeout** limita el tiempo maximo que una operacion puede tomar. Si la operacion no completa en ese tiempo, se cancela y se considera fallida.

| Propiedad | Tipo | Descripcion |
|-----------|------|-------------|
| `TimeoutSeconds` | `int` | Tiempo maximo en segundos para completar la operacion |

> **¿Por que un timeout de 30 segundos?** RabbitMQ normalmente responde en milisegundos. Si una operacion toma mas de 30 segundos, algo esta mal: la red esta caida, el broker esta sobrecargado, o hay un problema de configuracion. Sin timeout, la operacion podria quedarse colgada indefinidamente, bloqueando el worker.

---

## Arquitectura: Pipeline de Resiliencia en MassTransit

Los tres patrones se aplican como **middleware** en el pipeline de MassTransit, en el orden correcto para maximizar la proteccion:

```
┌─────────────────────────────────────────────────────────────────┐
│                     MassTransit Pipeline                        │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  1. Timeout                                               │  │
│  │     Limita el tiempo maximo de la operacion               │  │
│  │     Si excede → Cancela y lanza TimeoutException          │  │
│  └──────────────────────────┬────────────────────────────────┘  │
│                              │                                  │
│                              ▼                                  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  2. Circuit Breaker                                       │  │
│  │     Monitorea fallos en ventana de tiempo                 │  │
│  │     Si threshold → Abre circuito, rechaza llamadas        │  │
│  └──────────────────────────┬────────────────────────────────┘  │
│                              │                                  │
│                              ▼                                  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  3. Retry (Exponential Backoff)                           │  │
│  │     Reintenta con backoff exponencial                     │  │
│  │     Si MaxRetryCount falla → Propaga la excepcion         │  │
│  └──────────────────────────┬────────────────────────────────┘  │
│                              │                                  │
│                              ▼                                  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  4. Publicacion a RabbitMQ                                │  │
│  │     Envio real del mensaje al broker                      │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

> **¿Por que este orden?** El timeout va primero porque es la proteccion mas basica: no esperar indefinamente. El circuit breaker va despues del timeout porque necesita contar los fallos que ya incluyen timeouts. El retry va al final (antes del envio real) porque es el ultimo recurso antes de fallar. Este orden garantiza que cada capa protege a la siguiente.

---

## Configuracion

### Estructura del Archivo de Configuracion

 [`SoftwareLearningGuide.OutboxProcessor/appsettings.json`](SoftwareLearningGuide.OutboxProcessor/appsettings.json)

```json
{
  "ResilienceConfiguration": {
    "MessageBrokerApi": {
      "Retry": {
        "MaxRetryCount": 3,
        "InitialRetryIntervalSeconds": 1,
        "MaxRetryIntervalSeconds": 30,
        "IntervalDeltaSeconds": 5
      },
      "CircuitBreaker": {
        "TripThreshold": 15,
        "ActiveThreshold": 10,
        "TrackingPeriodMinutes": 1,
        "ResetIntervalMinutes": 5
      },
      "TimeOut": {
        "TimeoutSeconds": 30
      }
    }
  }
}
```

### Tabla de Valores por Entorno

| Propiedad | Desarrollo | Produccion | Descripcion |
|-----------|------------|------------|-------------|
| `Retry.MaxRetryCount` | 3 | 5 | Numero maximo de reintentos |
| `Retry.InitialRetryIntervalSeconds` | 1 | 2 | Intervalo inicial antes del primer reintento |
| `Retry.MaxRetryIntervalSeconds` | 30 | 120 | Intervalo maximo entre reintentos |
| `Retry.IntervalDeltaSeconds` | 5 | 10 | Incremento entre cada reintento |
| `CircuitBreaker.TripThreshold` | 15 | 10 | Fallos para abrir el circuito |
| `CircuitBreaker.ActiveThreshold` | 10 | 5 | Llamadas minimas antes de evaluar |
| `CircuitBreaker.TrackingPeriodMinutes` | 1 | 2 | Ventana de tiempo para contar fallos |
| `CircuitBreaker.ResetIntervalMinutes` | 5 | 3 | Tiempo para intentar HALF-OPEN |
| `TimeOut.TimeoutSeconds` | 30 | 10 | Tiempo maximo por operacion |

> **En desarrollo usamos valores permisivos** porque los servicios locales son estables. **En produccion usamos valores mas estrictos** porque los fallos de red son mas frecuentes y necesitamos proteger el broker mas agresivamente.

---

## Implementacion en Codigo

### Opciones de Configuracion

[`SoftwareLearningGuide.OutboxProcessor/Options/ResilienceOptions.cs`](SoftwareLearningGuide.OutboxProcessor/Options/ResilienceOptions.cs)

```csharp
namespace SoftwareLearningGuide.OutboxProcessor.Options;

public sealed class ResilienceOptions {
    public static string SectionName => "ResilienceConfiguration";
    public MessageBrokerResilienceOptions MessageBrokerApi { get; set; } = new();
}

public sealed class MessageBrokerResilienceOptions {
    public RetryPolicyOptions Retry { get; set; } = new();
    public CircuitBreakerPolicyOptions CircuitBreaker { get; set; } = new();
    public TimeoutPolicyOptions TimeOut { get; set; } = new();
}

public sealed class RetryPolicyOptions {
    public int MaxRetryCount { get; set; } = 3;
    public int InitialRetryIntervalSeconds { get; set; } = 1;
    public int MaxRetryIntervalSeconds { get; set; } = 30;
    public int IntervalDeltaSeconds { get; set; } = 5;
}

public sealed class CircuitBreakerPolicyOptions {
    public int TripThreshold { get; set; } = 15;
    public int ActiveThreshold { get; set; } = 10;
    public int TrackingPeriodMinutes { get; set; } = 1;
    public int ResetIntervalMinutes { get; set; } = 5;
}

public sealed class TimeoutPolicyOptions {
    public int TimeoutSeconds { get; set; } = 30;
}
```

> **¿Por que clases `sealed`?** En .NET, las clases sealed son ligeramente mas performantes porque el JIT puede optimizar las llamadas virtuales. Ademas, transmiten la intencion de que estas clases no deben heredarse: son contratos de configuracion, no extensiones.

### Binding de Opciones

[`SoftwareLearningGuide.OutboxProcessor/Extensions/OptionConfigurationServiceCollectionExtensions.cs`](SoftwareLearningGuide.OutboxProcessor/Extensions/OptionConfigurationServiceCollectionExtensions.cs)

```csharp
public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration) {
    // ... otras opciones ...
    services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
    return services;
}

public static ResilienceOptions GetResilienceOptions(this IConfiguration configuration) {
    return configuration
        .GetSection(ResilienceOptions.SectionName)
        .Get<ResilienceOptions>() ?? new ResilienceOptions();
}
```

### Aplicacion en MassTransit

[`SoftwareLearningGuide.OutboxProcessor/Program.cs`](SoftwareLearningGuide.OutboxProcessor/Program.cs)

```csharp
var resilienceOptions = builder.Configuration.GetResilienceOptions();

var retryPolicy = resilienceOptions.MessageBrokerApi.Retry;
var circuitBreakerPolicy = resilienceOptions.MessageBrokerApi.CircuitBreaker;
var timeoutPolicy = resilienceOptions.MessageBrokerApi.TimeOut;

builder.Services.AddMassTransit(x => {
    x.UsingRabbitMq((context, cfg) => {
        cfg.Host(messageBrokerOptions.Host, "/", h => {
            h.Username(messageBrokerOptions.Username);
            h.Password(messageBrokerOptions.Password);
        });

        // 1. Retry: backoff exponencial configurable
        cfg.UseMessageRetry(r => r.Exponential(
            retryPolicy.MaxRetryCount,
            TimeSpan.FromSeconds(retryPolicy.InitialRetryIntervalSeconds),
            TimeSpan.FromSeconds(retryPolicy.MaxRetryIntervalSeconds),
            TimeSpan.FromSeconds(retryPolicy.IntervalDeltaSeconds)));

        // 2. Circuit Breaker: proteccion contra fallos sostenidos
        cfg.UseCircuitBreaker(cb => {
            cb.TripThreshold = circuitBreakerPolicy.TripThreshold;
            cb.ActiveThreshold = circuitBreakerPolicy.ActiveThreshold;
            cb.TrackingPeriod = TimeSpan.FromMinutes(circuitBreakerPolicy.TrackingPeriodMinutes);
            cb.ResetInterval = TimeSpan.FromMinutes(circuitBreakerPolicy.ResetIntervalMinutes);
        });

        // 3. Timeout: limite de tiempo por operacion
        cfg.UseTimeout(t => t.Timeout = TimeSpan.FromSeconds(timeoutPolicy.TimeoutSeconds));

        cfg.ConfigureEndpoints(context);
    });
});
```

---

## Ejemplos: Que Sucede ante un Fallo

### Escenario 1: RabbitMQ se cae por 2 minutos

```
Segundo 0:  RabbitMQ se cae
            → OutboxProcessor intenta publicar
            → Intento 1: FALLO (0s)
            → Intento 2: FALLO (1s despues)
            → Intento 3: FALLO (6s despues)
            → Intento 4: FALLO (11s despues)
            → Intento 5: FALLO (16s despues)
            → Max reintentos alcanzado, propaga excepcion

Segundo 60: RabbitMQ se recupera
            → OutboxProcessor reintenta en el proximo ciclo (5s)
            → Intento 1: OK
            → Mensaje publicado exitosamente
```

**Resultado:** El mensaje se entrega con un retraso maximo de ~2 minutos. No hay perdida de datos.

### Escenario 2: RabbitMQ caido por 10 minutos (Circuit Breaker se activa)

```
Minuto 0:   RabbitMQ se cae
            → OutboxProcessor intenta publicar
            → FALLOS SUcesivos → Circuit Breaker cuenta en TrackingPeriod

Minuto 1:   TripThreshold alcanzado (15 fallos en 1 minuto)
            → Circuit Breaker ABRIR → Rechaza llamadas inmediatamente
            → No mas intentos fallidos, se ahorran recursos

Minuto 5:   ResetInterval expira
            → Circuit Breaker HALF-OPEN → Prueba con 1 llamada
            → FALLO (RabbitMQ sigue caido)
            → Circuit Breaker vuelve a ABRIR

Minuto 8:   RabbitMQ se recupera
            → ResetInterval expira
            → Circuit Breaker HALF-OPEN → Prueba con 1 llamada
            → OK
            → Circuit Breaker CIERRA → Vuelve a funcionar normal

Minuto 9:   OutboxProcessor procesa mensajes pendientes
            → Todos los mensajes de la outbox se publican
```

**Resultado:** El circuit breaker previene cientos de reintentos inutiles. Cuando RabbitMQ se recupera, el trafico vuelve automaticamente.

### Escenario 3: RabbitMQ responde lentamente (Timeout)

```
Intento 1:  OutboxProcessor publica
            → RabbitMQ no responde en 30 segundos
            → Timeout Cancela la operacion
            → FALLO: TimeoutException

Intento 2:  OutboxProcessor reintenta (1s despues)
            → RabbitMQ responde en 200ms
            → OK
```

**Resultado:** El timeout evita que el worker se quede colgado. Si RabbitMQ esta lento pero no caido, el retry con backoff resuelve el problema.

---

## Estructura de Archivos

```
SoftwareLearningGuide.OutboxProcessor/
├── Options/
│   ├── ResilienceOptions.cs           ← Clases de configuracion de resiliencia
│   ├── DatabaseOptions.cs
│   ├── MessageBrokerOptions.cs
│   └── OpenTelemetryOptions.cs
├── Extensions/
│   └── OptionConfigurationServiceCollectionExtensions.cs  ← Binding de ResilienceOptions
├── Workers/
│   └── CustomOutboxProcessorWorker.cs ← Worker que usa IPublishEndpoint (resiliencia aplicada)
├── Program.cs                         ← Aplica politicas al pipeline de MassTransit
├── appsettings.json                   ← Template de produccion (valores vacios)
├── appsettings.Development.json       ← Valores de desarrollo
└── README.Resilience.md               ← Este archivo
```

### Archivos Relacionados

| Archivo | Rol |
|---------|-----|
| [`ResilienceOptions.cs`](SoftwareLearningGuide.OutboxProcessor/Options/ResilienceOptions.cs) | Define las opciones de retry, circuit breaker y timeout |
| [`OptionConfigurationServiceCollectionExtensions.cs`](SoftwareLearningGuide.OutboxProcessor/Extensions/OptionConfigurationServiceCollectionExtensions.cs) | Registra y lee las opciones desde appsettings |
| [`Program.cs`](SoftwareLearningGuide.OutboxProcessor/Program.cs) | Aplica las politicas al pipeline de MassTransit |
| [`CustomOutboxProcessorWorker.cs`](SoftwareLearningGuide.OutboxProcessor/Workers/CustomOutboxProcessorWorker.cs) | Worker que publica mensajes via IPublishEndpoint |

---

## Recursos Recomendados

- [MassTransit - Message Retry Configuration](https://masstransit.io/documentation/configuration/middleware/retry)
- [MassTransit - Circuit Breaker Configuration](https://masstransit.io/documentation/configuration/middleware/circuit-breaker)
- [Martin Fowler - Circuit Breaker Pattern](http://martinfowler.com/bliki/CircuitBreaker.html)
- [Polly - Resilience and Transient Fault Handling](https://github.com/App-vNext/Polly)
- [Exponential Backoff and Jitter - AWS Architecture Blog](https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/)
- [Michael T. Nygard - Release It!](https://pragprog.com/titles/mnee2/release-it-second-edition/)

---

**Ultima actualizacion:** 2026
**Proyecto:** SoftwareLearningGuide.OutboxProcessor
