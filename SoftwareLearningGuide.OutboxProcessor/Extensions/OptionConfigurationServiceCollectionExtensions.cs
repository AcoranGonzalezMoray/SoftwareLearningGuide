using SoftwareLearningGuide.OutboxProcessor.Options;

namespace SoftwareLearningGuide.OutboxProcessor.Extensions;

public static class OptionConfigurationServiceCollectionExtensions {
    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<MessageBrokerOptions>(configuration.GetSection(MessageBrokerOptions.SectionName));
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));

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

    public static MessageBrokerOptions GetMessageBrokerOptions(this IConfiguration configuration) {
        return configuration
            .GetSection(MessageBrokerOptions.SectionName)
            .Get<MessageBrokerOptions>() ?? new MessageBrokerOptions();
    }

    public static ResilienceOptions GetResilienceOptions(this IConfiguration configuration) {
        return configuration
            .GetSection(ResilienceOptions.SectionName)
            .Get<ResilienceOptions>() ?? new ResilienceOptions();
    }
}
