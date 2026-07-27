namespace SoftwareLearningGuide.Application.Command.Ports;

/// <summary>
/// Puerto para registrar mensajes en la tabla Outbox transaccional.
/// </summary>
public interface IOutboxWriter {
    Task WriteAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
