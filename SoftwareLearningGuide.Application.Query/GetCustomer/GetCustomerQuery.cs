using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Query.GetCustomer;

/// <summary>
/// Query para obtener un Customer por su ID.
/// </summary>
public sealed record GetCustomerQuery(Guid CustomerId) : IRequest<Result<GetCustomerQueryResponse>>;
