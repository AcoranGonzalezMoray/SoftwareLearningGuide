using SoftwareLearningGuide.Api.FeatureToggles;
using SoftwareLearningGuide.Api.Options;

namespace SoftwareLearningGuide.Api.Extensions {
    public static class ConfigurationBuilderExtensions {
        public static IConfigurationBuilder AddFeatureManagementConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration configuration) {
            AddConfigurationFromFlagApi(configurationBuilder, configuration);

            return configurationBuilder;
        }

        private static void AddConfigurationFromFlagApi(IConfigurationBuilder configurationBuilder, IConfiguration configuration) {
            var flagsApi = configuration
                            .GetSection(FeatureManagementApiConfigurationOptions.SectionName)
                            .Get<FeatureManagementApiConfigurationOptions>() ?? new FeatureManagementApiConfigurationOptions();

            if (!string.IsNullOrEmpty(flagsApi.ApiUrl))
                configurationBuilder.Add(new FeatureManagementSourceConfiguration(flagsApi.ApiUrl, flagsApi.ApiKey, flagsApi.ReloadIntervalSeconds));

        }
    }
}
