using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Infraestructure.Data.Context;
using SoftwareLearningGuide.Infraestructure.Services;

namespace SoftwareLearningGuide.Infraestructure;

/// <summary>
/// Registro de dependencias para la capa de Infrastructure.
/// Configura Entity Framework (Command) y IOutboxWriter.
/// La API NO tiene dependencia de MassTransit ni RabbitMQ.
/// </summary>
public static class Dependencies {
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString) {
        // EF Core DbContext
        services.AddDbContext<ApplicationDbContext>(options => {
            options.UseSqlServer(connectionString);
        });

        // Registrar Outbox Writer personalizado
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        return services;
    }
}
