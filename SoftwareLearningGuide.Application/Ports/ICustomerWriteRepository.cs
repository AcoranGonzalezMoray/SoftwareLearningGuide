using SoftwareLearningGuide.Core.Business.Entities;

namespace SoftwareLearningGuide.Application.Command.Ports;

/// <summary>
/// Puerto de escritura para la entidad Customer.
/// Hereda operaciones comunes de IBaseRepository.
/// </summary>
public interface ICustomerWriteRepository : IBaseRepository<Customer, Guid> {
}
