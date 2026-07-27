using MediatR;
using Microsoft.Extensions.Logging;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Infraestructure.Data.Repositories;

/// <summary>
/// Implementación concreta de IUnitOfWork.
/// Envuelve el ApplicationDbContext y garantiza que todas las operaciones
/// de repositorio se persistan en una única transacción SQL.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork {
    private readonly ApplicationDbContext _context;
    private readonly IMediator _mediator;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(ApplicationDbContext context, IMediator mediator, ILogger<UnitOfWork> logger) {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Persiste todos los cambios acumulados en el contexto actual.
    /// Despacha los Domain Events antes de SaveChangesAsync para que cualquier Integration Event
    /// generado por los handlers se incluya como fila en DomainOutboxMessages dentro de la misma transacción.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
        await DispatchDomainEventsAsync(cancellationToken);

        var trackedEntries = _context.ChangeTracker.Entries()
            .Select(e => $"{e.Entity.GetType().Name} (Estado: {e.State})")
            .ToList();

        _logger.LogInformation(
            "[UnitOfWork] Guardando cambios. Entidades en ChangeTracker ({Count}): {Entries}",
            trackedEntries.Count, string.Join(", ", trackedEntries));

        return await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken) {
        while (true) {
            var aggregateRoots = _context.ChangeTracker
                .Entries<ProduceEvents>()
                .Where(e => e.Entity != null && e.Entity.DomainEvents != null && e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            if (aggregateRoots.Count == 0)
                break;

            foreach (var aggregate in aggregateRoots) {
                if (aggregate is null) continue;

                var domainEvents = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();

                foreach (var domainEvent in domainEvents) {
                    if (domainEvent is not null) {
                        await _mediator.Publish(domainEvent, cancellationToken);
                    }
                }
            }
        }
    }
}
