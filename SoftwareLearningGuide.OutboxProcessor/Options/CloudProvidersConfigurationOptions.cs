namespace SoftwareLearningGuide.OutboxProcessor.Options {
    public class CloudProvidersConfigurationOptions {
        public static string SectionName => "CloudProvidersConfigurations";

        public AwsConfigurationOptions AWS { get; set; } = new();
    }
}
