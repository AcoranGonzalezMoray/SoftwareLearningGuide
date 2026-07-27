using MediatR;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Application.Command.CreateCustomer;

/// <summary>
/// Handler de MediatR para CreateCustomerCommand.
/// Usa IUnitOfWork para garantizar que toda la operación se persista en una única transacción.
/// </summary>
public sealed class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>> {
    private readonly ICustomerWriteRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(ICustomerWriteRepository repository, IUnitOfWork unitOfWork) {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken) {
        var emailResult = Email.Create(request.Email);
        if (!emailResult.IsSuccess)
            return Result<Guid>.Failure(emailResult.Error!);

        Address? defaultAddress = null;
        if (!string.IsNullOrWhiteSpace(request.DefaultStreet)) {
            var addressResult = Address.Create(
                request.DefaultStreet,
                request.DefaultCity ?? string.Empty,
                request.DefaultState ?? string.Empty,
                request.DefaultPostalCode ?? string.Empty,
                request.DefaultCountry ?? string.Empty);

            if (!addressResult.IsSuccess)
                return Result<Guid>.Failure(addressResult.Error!);

            defaultAddress = addressResult.Value!;
        }

        var customerId = CustomerId.Create();
        var customerResult = Customer.Create(customerId, request.FirstName, request.LastName, emailResult.Value!);
        if (!customerResult.IsSuccess)
            return Result<Guid>.Failure(customerResult.Error!);

        var customer = customerResult.Value!;

        if (defaultAddress is not null)
            customer.SetDefaultShippingAddress(defaultAddress);

        await _repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(customer.Id.Value);
    }
}
