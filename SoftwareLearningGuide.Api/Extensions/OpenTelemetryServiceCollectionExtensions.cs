using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SoftwareLearningGuide.Api.Options;

namespace SoftwareLearningGuide.Api.Extensions {
    public static class OpenTelemetryServiceCollectionExtensions {
        public static IServiceCollection AddCustomOpenTelemetry(
            this IServiceCollection services,
            ILoggingBuilder logging,
            IConfiguration configuration,
            OpenTelemetryOptions otelOptions) {

            var otlpEndpoint = otelOptions.Otlp.Endpoint;
            OtlpExportProtocol otlpProtocol = GetOtlpExportProtocol(otelOptions.Otlp.Protocol);

            // 1. Trazas y Métricas
            var builder = services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(otelOptions.ServiceName))
                .WithTracing(tracing => {
                    tracing
                        .AddAspNetCoreInstrumentation()
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
                        .AddAspNetCoreInstrumentation();

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

        private static OtlpExportProtocol GetOtlpExportProtocol(string otlpProtocolConfig) {
            return otlpProtocolConfig.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                               otlpProtocolConfig.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase)
                ? OtlpExportProtocol.HttpProtobuf
                : OtlpExportProtocol.Grpc;
        }
    }
}