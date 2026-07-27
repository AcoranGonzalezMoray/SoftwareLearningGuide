using SoftwareLearningGuide.Api.Metrics;

namespace SoftwareLearningGuide.Api.Startup {
    public static class MetricsStartup {
        public static IServiceCollection AddCustomMetrics(this IServiceCollection services) {
            services.AddSingleton<OrderMetrics>();
            return services;
        }
    }
}
