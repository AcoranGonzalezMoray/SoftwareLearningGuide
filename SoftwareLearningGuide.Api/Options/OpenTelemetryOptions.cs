namespace SoftwareLearningGuide.Api.Options {
    public class OpenTelemetryOptions {
        public static string SectionName => "OpenTelemetry";
        public OtlpOptions Otlp { get; set; } = new OtlpOptions();
        public string ServiceName { get; set; } = string.Empty;

        public class OtlpOptions {
            public string? Endpoint { get; set; }

            public string? Protocol { get; set; } = "grpc";
        }
    }
}
