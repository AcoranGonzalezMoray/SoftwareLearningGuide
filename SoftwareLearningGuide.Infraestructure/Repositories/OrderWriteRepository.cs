using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Aggregates;
using SoftwareLearningGuide.Core.Business.ValueObjects;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Infraestructure.Data.Repositories;

/// <summary>
/// Implementación del puerto de escritura para Order usando Entity Framework.
/// </summary>
public sealed class OrderWriteRepository : BaseRepository<Order, OrderId, Guid>, IOrderWriteRepository {
    public OrderWriteRepository(ApplicationDbContext context)
        : base(context, guid => new OrderId(guid)) {
    }
}