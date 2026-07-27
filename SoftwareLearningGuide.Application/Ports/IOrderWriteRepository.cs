using SoftwareLearningGuide.Core.Business.Aggregates;

namespace SoftwareLearningGuide.Application.Command.Ports;

/// <summary>
/// Puerto de escritura para la entidad Order.
/// Hereda operaciones comunes de IBaseRepository.
/// </summary>
public interface IOrderWriteRepository : IBaseRepository<Order, Guid> {
}
