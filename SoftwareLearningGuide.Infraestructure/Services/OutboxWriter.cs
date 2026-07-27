using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Infraestructure.Data.Context;
using SoftwareLearningGuide.Infraestructure.Data.Entities;
using System.Text.Json;

namespace SoftwareLearningGuide.Infraestructure.Services;

/// <summary>
/// Implementación de IOutboxWriter que persiste los Integration Events
/// como entidades OutboxMessageEntity en la base de datos.
/// </summary>
public sealed class OutboxWriter : IOutboxWriter {
    private readonly ApplicationDbContext _context;

    public OutboxWriter(ApplicationDbContext context) {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task WriteAsync<T>(T message, CancellationToken cancellationToken = default) where T : class {
        var typeName = message.GetType().Name;

        var entity = new OutboxMessageEntity {
            Id = Guid.NewGuid(),
            Type = typeName,
            Content = JsonSerializer.Serialize(message),
            CreatedOnUtc = DateTime.UtcNow,
            ProcessedOnUtc = null,
            Error = null
        };

        _context.OutboxMessages.Add(entity);
        return Task.CompletedTask;
    }
}
