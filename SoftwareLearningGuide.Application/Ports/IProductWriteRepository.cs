using SoftwareLearningGuide.Core.Business.Entities;

namespace SoftwareLearningGuide.Application.Command.Ports;

/// <summary>
/// Puerto de escritura para la entidad Product.
/// Hereda operaciones comunes de IBaseRepository.
/// </summary>
public interface IProductWriteRepository : IBaseRepository<Product, Guid> {
}
