using MediatR;
using SoftwareLearningGuide.Application.Query.GetOrderQuery;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Query.GetOrder;

/// <summary>
/// Query para obtener una Order por su ID.
/// Implementa IRequest para MediatR: el controller despacha, el handler resuelve.
/// </summary>
public sealed record GetOrderQuery(Guid OrderId) : IRequest<Result<GetOrderQueryResponse>>;
