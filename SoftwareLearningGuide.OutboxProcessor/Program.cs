using MassTransit;
using Microsoft.Data.SqlClient;
using SoftwareLearningGuide.OutboxProcessor.Extensions;
using SoftwareLearningGuide.OutboxProcessor.Workers;
using System.Data;

var builder = Host.CreateApplicationBuilder(args);

// SSM Parameter Store (MiniStack/AWS): prioriza sobre appsettings; appsettings actúa como fallback
builder.Configuration.AddSystemsManagerConfiguration(builder.Configuration);

// =========================================================
// 1. OPTIONS
// =========================================================

builder.Services.AddOptions(builder.Configuration);

var databaseOptions = builder.Configuration.GetDatabaseOptions();
var messageBrokerOptions = builder.Configuration.GetMessageBrokerOptions();
var otelOptions = builder.Configuration.GetOpenTelemetryOptions();
var resilienceOptions = builder.Configuration.GetResilienceOptions();

// =========================================================
// 2. OBSERVABILITY (OpenTelemetry)
// =========================================================

builder.Services.AddCustomOpenTelemetry(builder.Logging, builder.Configuration, otelOptions);

// =========================================================
// 3. DAPPER (misma DB que la API principal)
// =========================================================

builder.Services.AddSingleton<IDbConnection>(_ => new SqlConnection(databaseOptions.SoftwareLearningGuide));

// =========================================================
// 4. MASSTRANSIT (Publicador hacia RabbitMQ)
// =========================================================

var retryPolicy = resilienceOptions.MessageBrokerApi.Retry;
var circuitBreakerPolicy = resilienceOptions.MessageBrokerApi.CircuitBreaker;
var timeoutPolicy = resilienceOptions.MessageBrokerApi.TimeOut;

builder.Services.AddMassTransit(x => {
    x.UsingRabbitMq((context, cfg) => {
        cfg.Host(messageBrokerOptions.Host, "/", h => {
            h.Username(messageBrokerOptions.Username);
            h.Password(messageBrokerOptions.Password);
        });

        cfg.UseMessageRetry(r => r.Exponential(
            retryPolicy.MaxRetryCount,
            TimeSpan.FromSeconds(retryPolicy.InitialRetryIntervalSeconds),
            TimeSpan.FromSeconds(retryPolicy.MaxRetryIntervalSeconds),
            TimeSpan.FromSeconds(retryPolicy.IntervalDeltaSeconds)));

        cfg.UseCircuitBreaker(cb => {
            cb.TripThreshold = circuitBreakerPolicy.TripThreshold;
            cb.ActiveThreshold = circuitBreakerPolicy.ActiveThreshold;
            cb.TrackingPeriod = TimeSpan.FromMinutes(circuitBreakerPolicy.TrackingPeriodMinutes);
            cb.ResetInterval = TimeSpan.FromMinutes(circuitBreakerPolicy.ResetIntervalMinutes);
        });

        cfg.UseTimeout(t => t.Timeout = TimeSpan.FromSeconds(timeoutPolicy.TimeoutSeconds));

        cfg.ConfigureEndpoints(context);
    });
});

// =========================================================
// 5. WORKER BACKGROUND SERVICE
// =========================================================

builder.Services.AddHostedService<CustomOutboxProcessorWorker>();

// =========================================================
// 6. BUILD & RUN
// =========================================================

var host = builder.Build();
host.Run();
