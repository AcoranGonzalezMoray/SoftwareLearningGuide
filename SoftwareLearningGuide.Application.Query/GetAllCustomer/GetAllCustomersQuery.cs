using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Query.GetAllCustomer;

/// <summary>
/// Query para obtener todos los Customers.
/// </summary>
public sealed record GetAllCustomersQuery : IRequest<Result<GetAllCustomersQueryResponse>>;
