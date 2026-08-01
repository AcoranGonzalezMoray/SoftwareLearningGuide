using MassTransit;
using SoftwareLearningGuide.Consumer.Consumers;
using SoftwareLearningGuide.Consumer.Extensions;
using SoftwareLearningGuide.Consumer.Metrics;

var builder = Host.CreateApplicationBuilder(args);

// SSM Parameter Store (MiniStack/AWS): prioriza sobre appsettings; appsettings actúa como fallback
builder.Configuration.AddSystemsManagerConfiguration(builder.Configuration);

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
// 3.1 MASSTRANSIT (Bus AWS SQS/SNS - consumidores extra)
// =========================================================

builder.Services.AddAwsMessageBus(builder.Configuration, x => {
    x.AddConsumer<CustomerCreatedConsumer>();
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
