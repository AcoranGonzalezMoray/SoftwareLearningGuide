using Dapper;
using MassTransit;
using SoftwareLearningGuide.Contracts.IntegrationEvents;
using System.Data;
using System.Text.Json;

namespace SoftwareLearningGuide.OutboxProcessor.Workers;

public sealed partial class CustomOutboxProcessorWorker : BackgroundService {
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomOutboxProcessorWorker> _logger;

    private static readonly Dictionary<string, Type> EventTypes = new() {
        [nameof(ProductCreatedEvent)] = typeof(ProductCreatedEvent),
        [nameof(OrderCreatedEvent)] = typeof(OrderCreatedEvent),
        [nameof(CustomerCreatedEvent)] = typeof(CustomerCreatedEvent),
        [nameof(ProductStockLowEvent)] = typeof(ProductStockLowEvent),
        [nameof(OrderCancelledEvent)] = typeof(OrderCancelledEvent),
    };

    private const string SelectPending = """
        SELECT TOP 20 Id, Type, Content, CreatedOnUtc
        FROM DomainOutboxMessages
        WHERE ProcessedOnUtc IS NULL
        ORDER BY CreatedOnUtc
        """;

    private const string MarkProcessed = """
        UPDATE DomainOutboxMessages
        SET ProcessedOnUtc = @ProcessedOnUtc, Error = @Error
        WHERE Id = @Id
        """;

    public CustomOutboxProcessorWorker(
        IServiceProvider serviceProvider,
        ILogger<CustomOutboxProcessorWorker> logger) {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("[OutboxProcessorWorker] Iniciado escaneo de DomainOutboxMessages...");

        while (!stoppingToken.IsCancellationRequested) {
            try {
                using var scope = _serviceProvider.CreateScope();
                var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var pendingMessages = (await connection.QueryAsync<SelectOutboxMessage>(SelectPending)).ToList();

                if (pendingMessages.Count > 0) {
                    _logger.LogInformation("[OutboxProcessorWorker] Procesando {Count} mensajes de la Outbox...", pendingMessages.Count);

                    foreach (var message in pendingMessages) {
                        try {
                            if (!EventTypes.TryGetValue(message.Type, out var messageType)) {
                                _logger.LogWarning("[OutboxProcessorWorker] Tipo desconocido {Type}", message.Type);
                                await MarkMessageAsync(connection, message.Id, DateTime.UtcNow, null);
                                continue;
                            }

                            var deserializedObj = JsonSerializer.Deserialize(message.Content, messageType);
                            if (deserializedObj != null) {
                                await publishEndpoint.Publish(deserializedObj, messageType, stoppingToken);
                                _logger.LogInformation("[OutboxProcessorWorker] Publicado {Type} (Id: {Id})", message.Type, message.Id);
                            }

                            await MarkMessageAsync(connection, message.Id, DateTime.UtcNow, null);
                        }
                        catch (Exception ex) {
                            _logger.LogError(ex, "[OutboxProcessorWorker] Error procesando mensaje Outbox Id {Id}", message.Id);
                            await MarkMessageAsync(connection, message.Id, DateTime.UtcNow, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested) {
                _logger.LogError(ex, "[OutboxProcessorWorker] Error en el ciclo de procesamiento de la Outbox");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private static async Task MarkMessageAsync(IDbConnection connection, Guid id, DateTime processedOnUtc, string? error) {
        await connection.ExecuteAsync(MarkProcessed, new { Id = id, ProcessedOnUtc = processedOnUtc, Error = error });
    }
}
