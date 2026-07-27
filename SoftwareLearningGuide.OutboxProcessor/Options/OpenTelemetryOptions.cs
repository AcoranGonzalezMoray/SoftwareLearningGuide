namespace SoftwareLearningGuide.OutboxProcessor.Options;

public sealed class OpenTelemetryOptions {
    public static string SectionName => "OpenTelemetry";
    public string ServiceName { get; set; } = string.Empty;
    public OtlpOptions Otlp { get; set; } = new();

    public sealed class OtlpOptions {
        public string? Endpoint { get; set; }
        public string? Protocol { get; set; } = "grpc";
    }
}
