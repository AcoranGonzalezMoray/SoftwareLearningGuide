using Asp.Versioning;

namespace SoftwareLearningGuide.Api.Extensions {
    public static class ApiVersioningServiceCollectionExtensions {
        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services) {
            // Registrar servicio de versionado
            services.AddApiVersioning(options => {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            }).AddApiExplorer(options => {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // Registrar los documentos OpenAPI explícitos
            services.AddOpenApi("v1");
            services.AddOpenApi("v2");

            return services;
        }
    }
}