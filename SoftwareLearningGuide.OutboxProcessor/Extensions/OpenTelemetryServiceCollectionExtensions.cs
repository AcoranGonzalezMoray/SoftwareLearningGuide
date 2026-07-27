using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SoftwareLearningGuide.OutboxProcessor.Options;

namespace SoftwareLearningGuide.OutboxProcessor.Extensions;

public static class OpenTelemetryServiceCollectionExtensions {
    public static IServiceCollection AddCustomOpenTelemetry(
        this IServiceCollection services,
        ILoggingBuilder logging,
        IConfiguration configuration,
        OpenTelemetryOptions otelOptions) {
        var otlpEndpoint = otelOptions.Otlp.Endpoint;
        var otlpProtocol = GetOtlpExportProtocol(otelOptions.Otlp.Protocol);

        // 1. Trazas y Metricas
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(otelOptions.ServiceName))
            .WithTracing(tracing => {
                tracing
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint)) {
                    tracing.AddOtlpExporter(o => {
                        o.Endpoint = new Uri(otlpEndpoint);
                        o.Protocol = otlpProtocol;
                    });
                }
                else {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics => {
                metrics
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint)) {
                    metrics.AddOtlpExporter(o => {
                        o.Endpoint = new Uri(otlpEndpoint);
                        o.Protocol = otlpProtocol;
                    });
                }
            });

        // 2. Logging estructurado
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddOpenTelemetry(options => {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;

            if (!string.IsNullOrEmpty(otlpEndpoint)) {
                options.AddOtlpExporter(o => {
                    o.Endpoint = new Uri(otlpEndpoint);
                    o.Protocol = otlpProtocol;
                });
            }
            else {
                options.AddConsoleExporter();
            }
        });

        return services;
    }

    private static OtlpExportProtocol GetOtlpExportProtocol(string? otlpProtocolConfig) {
        return otlpProtocolConfig is not null &&
               (otlpProtocolConfig.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                otlpProtocolConfig.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase))
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;
    }
}
