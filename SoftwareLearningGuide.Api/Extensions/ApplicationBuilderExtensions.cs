using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using SoftwareLearningGuide.Api.FeatureToggles;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Api.Extensions {
    public static class ApplicationBuilderExtensions {
        public static IApplicationBuilder ApplyMigrations(this IApplicationBuilder app) {
            var featureManager = app.ApplicationServices.GetRequiredService<IFeatureManager>();
            if (featureManager.IsEnabledAsync(FeatureToggleNames.FT_APPLY_MIGRATIONS_ON_START).GetAwaiter().GetResult()) {
                using var scope = app.ApplicationServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }
            return app;
        }
    }
}
