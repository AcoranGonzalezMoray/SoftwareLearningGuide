using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Query.GetProduct;

/// <summary>
/// Query para obtener un Product por su ID.
/// Implementa IRequest para MediatR: el controller despacha, el handler resuelve.
/// </summary>
public sealed record GetProductQuery(Guid ProductId) : IRequest<Result<GetProductQueryResponse>>;
