using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.ValueObjects;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Infraestructure.Data.Repositories;

/// <summary>
/// Implementación del puerto de escritura para Customer usando Entity Framework.
/// </summary>
public sealed class CustomerWriteRepository : BaseRepository<Customer, CustomerId, Guid>, ICustomerWriteRepository {
    public CustomerWriteRepository(ApplicationDbContext context)
        : base(context, guid => CustomerId.From(guid).Value) {
    }
}
