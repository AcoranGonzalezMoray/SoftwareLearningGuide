namespace SoftwareLearningGuide.Application.Command.Ports;

/// <summary>
/// Puerto que define una Unidad de Trabajo.
/// Agrupa múltiples operaciones de repositorio en una única transacción.
/// Garantiza que todas las operaciones se committean (o revierten) juntas.
/// </summary>
public interface IUnitOfWork {
    /// <summary>
    /// Persiste todos los cambios acumulados en el contexto actual.
    /// Debe invocarse una sola vez al final del caso de uso (Command Handler).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Número de registros afectados.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
