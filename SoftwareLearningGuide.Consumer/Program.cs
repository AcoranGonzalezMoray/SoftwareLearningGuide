using MassTransit;
using SoftwareLearningGuide.Consumer.Consumers;
using SoftwareLearningGuide.Consumer.Extensions;
using SoftwareLearningGuide.Consumer.Metrics;

var builder = Host.CreateApplicationBuilder(args);

// =========================================================
// 1. OPTIONS
// =========================================================

builder.Services.AddOptions(builder.Configuration);

var messageBrokerOptions = builder.Configuration.GetMessageBrokerOptions();
var otelOptions = builder.Configuration.GetOpenTelemetryOptions();

// =========================================================
// 2. OBSERVABILITY (OpenTelemetry)
// =========================================================

builder.Services.AddCustomOpenTelemetry(builder.Logging, builder.Configuration, otelOptions);

// =========================================================
// 3. MASSTRANSIT (Consumer - solo escucha RabbitMQ)
// =========================================================

builder.Services.AddMassTransit(x => {
    x.AddConsumer<OrderCreatedConsumer>();
    x.AddConsumer<ProductCreatedConsumer>();
    x.AddConsumer<OrderCancelledConsumer>();
    x.AddConsumer<ProductStockLowConsumer>();

    x.UsingRabbitMq((context, cfg) => {
        cfg.Host(messageBrokerOptions.Host, "/", h => {
            h.Username(messageBrokerOptions.Username);
            h.Password(messageBrokerOptions.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// =========================================================
// 4. METRICS
// =========================================================

builder.Services.AddSingleton<ConsumerMetrics>();

// =========================================================
// 5. BUILD & RUN
// =========================================================

var host = builder.Build();
host.Run();
