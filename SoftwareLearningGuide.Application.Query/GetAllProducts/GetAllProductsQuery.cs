using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Query.GetAllProducts;

/// <summary>
/// Query para obtener todos los Products.
/// </summary>
public sealed record GetAllProductsQuery : IRequest<Result<GetAllProductsQueryResponse>>;
