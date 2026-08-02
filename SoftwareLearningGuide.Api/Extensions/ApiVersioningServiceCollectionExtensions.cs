using Asp.Versioning;

namespace SoftwareLearningGuide.Api.Extensions {
    public static class ApiVersioningServiceCollectionExtensions {
        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services) {
            services.AddApiVersioning(options => {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            }).AddApiExplorer(options => {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddCustomOpenApi();
            return services;
        }
    }
}