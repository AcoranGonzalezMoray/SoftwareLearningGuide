using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using SoftwareLearningGuide.Consumer.Options;

namespace SoftwareLearningGuide.Consumer.Extensions {
    public static class ConfigurationBuilderExtensions {
        public static IConfigurationBuilder AddSystemsManagerConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration configuration) {
            var cloudProvidersConfiguration = configuration
                .GetSection(CloudProvidersConfigurationOptions.SectionName)
                .Get<CloudProvidersConfigurationOptions>() ?? new CloudProvidersConfigurationOptions();

            if (cloudProvidersConfiguration.AWS.Enabled)
                AddAWSProvider(configurationBuilder, cloudProvidersConfiguration);

            return configurationBuilder;
        }

        private static IConfigurationBuilder AddAWSProvider(IConfigurationBuilder configurationBuilder, CloudProvidersConfigurationOptions cloudProvidersConfiguration) {
            var ssmOptions = cloudProvidersConfiguration.AWS.SSM;

            if (!ssmOptions.Enabled || string.IsNullOrEmpty(ssmOptions.Path))
                return configurationBuilder;

            var awsOptions = new AWSOptions {
                Credentials = new BasicAWSCredentials(
                    cloudProvidersConfiguration.AWS.Credentials.AccessKey,
                    cloudProvidersConfiguration.AWS.Credentials.AccessSecret),
                Region = RegionEndpoint.GetBySystemName(ssmOptions.Region)
            };

            if (!string.IsNullOrEmpty(ssmOptions.ServiceUrl))
                awsOptions.DefaultClientConfig.ServiceURL = ssmOptions.ServiceUrl;

            configurationBuilder.AddSystemsManager(source => {
                source.Path = ssmOptions.Path;
                source.Optional = true;
                source.ReloadAfter = TimeSpan.FromSeconds(ssmOptions.ReloadIntervalSeconds);
                source.AwsOptions = awsOptions;
            });

            return configurationBuilder;
        }
    }
}
