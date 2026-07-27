using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Infraestructure.Data.Repositories;

namespace SoftwareLearningGuide.Api.Startup {
    public static class ReporitoriesStartup {
        public static IServiceCollection AddRepositories(this IServiceCollection services) {
            services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
            services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
            services.AddScoped<ICustomerWriteRepository, CustomerWriteRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
