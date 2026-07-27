using Microsoft.Data.SqlClient;
using SoftwareLearningGuide.Api.Options;
using System.Data;

namespace SoftwareLearningGuide.Api.Startup {
    public static class DbConnectionsStartup {
        public static IServiceCollection AddDbConnections(this IServiceCollection services, DatabaseOptions databaseOptions) {
            services.AddScoped<IDbConnection>(_ => new SqlConnection(databaseOptions.SoftwareLearningGuide));
            return services;
        }
    }
}
