using Amazon.SQS;
using Amazon.SimpleNotificationService;
using MassTransit;
using SoftwareLearningGuide.OutboxProcessor.Buses;
using SoftwareLearningGuide.OutboxProcessor.Options;

namespace SoftwareLearningGuide.OutboxProcessor.Extensions;

public static class AwsMessageBusServiceCollectionExtensions {
    /// <summary>
    /// Registra el segundo bus de MassTransit con transporte AWS (SQS/SNS) solo para publicación.
    /// Solo se registra si AWS.Enabled y AWS.Messaging.Enabled son true.
    /// </summary>
    public static IServiceCollection AddAwsMessageBus(this IServiceCollection services, IConfiguration configuration) {
        var awsOptions = configuration
            .GetSection(CloudProvidersConfigurationOptions.SectionName)
            .Get<CloudProvidersConfigurationOptions>()?
            .AWS;

        if (awsOptions is null || !awsOptions.Enabled || !awsOptions.Messaging.Enabled) {
            return services;
        }

        services.AddMassTransit<IAwsMessageBus>(x => {
            x.UsingAmazonSqs((context, cfg) => {
                cfg.Host(awsOptions.Messaging.Region, h => {
                    h.AccessKey(awsOptions.Credentials.AccessKey);
                    h.SecretKey(awsOptions.Credentials.AccessSecret);

                    if (!string.IsNullOrEmpty(awsOptions.Messaging.ServiceUrl)) {
                        h.Config(new AmazonSQSConfig { ServiceURL = awsOptions.Messaging.ServiceUrl });
                        h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = awsOptions.Messaging.ServiceUrl });
                    }
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
