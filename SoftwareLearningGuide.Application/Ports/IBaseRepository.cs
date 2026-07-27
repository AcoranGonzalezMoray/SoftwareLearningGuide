namespace SoftwareLearningGuide.Application.Command.Ports;

/// <summary>
/// Puerto base genérico para persistencia de entidades.
/// Proporciona operaciones CRUD comunes: GetById y Add.
/// El guardado se realiza a través de IUnitOfWork para garantizar transaccionalidad.
/// </summary>
/// <typeparam name="TEntity">Tipo de la entidad.</typeparam>
/// <typeparam name="TId">Tipo del identificador.</typeparam>
public interface IBaseRepository<TEntity, TId> where TEntity : class {
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}
