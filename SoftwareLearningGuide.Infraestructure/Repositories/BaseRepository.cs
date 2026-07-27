using Microsoft.EntityFrameworkCore;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Infraestructure.Data.Context;

/// <summary>
/// Repositorio base abstracto con operaciones CRUD comunes.
/// Solo gestiona operaciones en memoria sobre el DbContext.
/// El guardado real se realiza a través de IUnitOfWork.
/// </summary>
public abstract class BaseRepository<TEntity, TId, TIdValue> : IBaseRepository<TEntity, TIdValue>
    where TEntity : class {
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    private readonly Func<TIdValue, TId> _idFactory;

    protected BaseRepository(ApplicationDbContext context, Func<TIdValue, TId> idFactory) {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
        _idFactory = idFactory ?? throw new ArgumentNullException(nameof(idFactory));
    }

    public virtual async Task<TEntity?> GetByIdAsync(TIdValue id, CancellationToken cancellationToken = default) {
        var typedId = _idFactory(id);

        return await _dbSet.FirstOrDefaultAsync(e => EF.Property<TId>(e, "Id").Equals(typedId), cancellationToken);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) {
        await _dbSet.AddAsync(entity, cancellationToken);
    }
}