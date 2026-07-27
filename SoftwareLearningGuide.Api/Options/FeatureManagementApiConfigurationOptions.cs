namespace SoftwareLearningGuide.Api.Options {
    public class FeatureManagementApiConfigurationOptions {
        public static string SectionName => "FeatureManagementApiConfiguration";
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int ReloadIntervalSeconds { get; set; } = 30;
    }
}
