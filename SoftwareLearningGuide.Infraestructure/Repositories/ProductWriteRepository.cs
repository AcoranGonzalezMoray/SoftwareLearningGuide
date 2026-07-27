using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.ValueObjects;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Infraestructure.Data.Repositories;

/// <summary>
/// Implementación del puerto de escritura para Product usando Entity Framework.
/// </summary>
public sealed class ProductWriteRepository : BaseRepository<Product, ProductId, Guid>, IProductWriteRepository {
    public ProductWriteRepository(ApplicationDbContext context)
        : base(context, guid => new ProductId(guid)) {
    }
}
