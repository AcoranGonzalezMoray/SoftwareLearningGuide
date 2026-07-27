using SoftwareLearningGuide.Api.Options;

namespace SoftwareLearningGuide.Api.Extensions {
    public static class OptionConfigurationServiceCollectionExtensions {
        public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration) {
            services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));
            services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
            services.Configure<FeatureManagementApiConfigurationOptions>(configuration.GetSection(FeatureManagementApiConfigurationOptions.SectionName));

            return services;
        }

        public static OpenTelemetryOptions GetOpenTelemetryOptions(this IConfiguration configuration) {
            return configuration
                .GetSection(OpenTelemetryOptions.SectionName)
                .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
        }

        public static DatabaseOptions GetDatabaseOptions(this IConfiguration configuration) {
            return configuration
                .GetSection(DatabaseOptions.SectionName)
                .Get<DatabaseOptions>() ?? new DatabaseOptions();
        }
    }
}
