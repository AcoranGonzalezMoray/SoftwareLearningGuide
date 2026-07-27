using Microsoft.FeatureManagement;
using SoftwareLearningGuide.Api.Extensions;
using SoftwareLearningGuide.Api.Middlewares;
using SoftwareLearningGuide.Api.Startup;
using SoftwareLearningGuide.Infraestructure;

namespace SoftwareLearningGuide.Api {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // =========================================================
            // 1. REGISTRO DE SERVICIOS
            // =========================================================

            builder.Services.AddControllers();

            builder.Services.AddOptions(builder.Configuration);

            // Feature Toggle/Flags: provider de flags (prioriza sobre appsettings)
            builder.Configuration.AddFeatureManagementConfiguration(builder.Configuration);
            builder.Services.AddFeatureManagement();

            // Módulo OpenTelemetry
            var otelOptions = builder.Configuration.GetOpenTelemetryOptions();
            builder.Services.AddCustomOpenTelemetry(builder.Logging, builder.Configuration, otelOptions);

            // Módulo Versionado y OpenAPI
            builder.Services.AddCustomApiVersioning();

            // CQRS: Infrastructure (EF Core DbContext + Domain OutboxWriter)
            var databaseOptions = builder.Configuration.GetDatabaseOptions();
            builder.Services.AddInfrastructure(databaseOptions.SoftwareLearningGuide);

            // CQRS: Repositories
            builder.Services.AddRepositories();

            // CQRS: MediatR (IMediator + handlers automáticamente)
            builder.Services.AddCQRS();

            // Métricas custom
            builder.Services.AddCustomMetrics();

            //DbConnections
            builder.Services.AddDbConnections(databaseOptions);

            // =========================================================
            // 2. CONSTRUCCIÓN DE LA APLICACIÓN
            // =========================================================

            var app = builder.Build();

            app.ApplyMigrations();

            // =========================================================
            // 3. PIPELINE DE PETICIONES HTTP (MIDDLEWARES)
            // =========================================================

            if (app.Environment.IsDevelopment()) {
                app.MapOpenApi();

                app.UseSwaggerUI(options => {
                    options.SwaggerEndpoint("/openapi/v1.json", "V1 Docs");
                    options.SwaggerEndpoint("/openapi/v2.json", "V2 Docs");
                });
            }

            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
