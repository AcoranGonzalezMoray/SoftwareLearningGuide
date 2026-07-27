using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Query.GetAllOrders;

/// <summary>
/// Query para obtener todas las Orders.
/// </summary>
public sealed record GetAllOrdersQuery : IRequest<Result<GetAllOrdersQueryResponse>>;
