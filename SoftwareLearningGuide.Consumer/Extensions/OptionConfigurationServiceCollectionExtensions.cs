using SoftwareLearningGuide.Consumer.Options;

namespace SoftwareLearningGuide.Consumer.Extensions;

public static class OptionConfigurationServiceCollectionExtensions {
    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));
        services.Configure<MessageBrokerOptions>(configuration.GetSection(MessageBrokerOptions.SectionName));

        return services;
    }

    public static OpenTelemetryOptions GetOpenTelemetryOptions(this IConfiguration configuration) {
        return configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
    }

    public static MessageBrokerOptions GetMessageBrokerOptions(this IConfiguration configuration) {
        return configuration
            .GetSection(MessageBrokerOptions.SectionName)
            .Get<MessageBrokerOptions>() ?? new MessageBrokerOptions();
    }
}
