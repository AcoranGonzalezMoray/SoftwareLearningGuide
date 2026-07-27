using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Application.Command.CreateCustomer;

/// <summary>
/// Command para crear un nuevo Customer.
/// </summary>
public sealed record CreateCustomerCommand : IRequest<Result<Guid>> {
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? DefaultStreet { get; init; }
    public string? DefaultCity { get; init; }
    public string? DefaultState { get; init; }
    public string? DefaultPostalCode { get; init; }
    public string? DefaultCountry { get; init; }
}
