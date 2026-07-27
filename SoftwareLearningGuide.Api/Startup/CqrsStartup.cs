using SoftwareLearningGuide.Application.Command.CreateOrder;
using SoftwareLearningGuide.Application.Query.GetOrder;

namespace SoftwareLearningGuide.Api.Startup {
    public static class CqrsStartup {
        public static IServiceCollection AddCQRS(this IServiceCollection services) {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
                typeof(CreateOrderCommandHandler).Assembly,
                typeof(GetOrderQueryHandler).Assembly));

            return services;
        }
    }
}
