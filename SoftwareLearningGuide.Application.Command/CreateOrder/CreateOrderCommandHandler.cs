using MediatR;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Aggregates;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Application.Command.CreateOrder;

/// <summary>
/// Handler de MediatR para CreateOrderCommand.
/// Valida reglas de negocio del dominio antes de persistir a través del repository.
/// Usa IUnitOfWork para garantizar que toda la operación se persista en una única transacción.
/// </summary>
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>> {
    private readonly IOrderWriteRepository _orderRepository;
    private readonly ICustomerWriteRepository _customerRepository;
    private readonly IProductWriteRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderWriteRepository orderRepository,
        ICustomerWriteRepository customerRepository,
        IProductWriteRepository productRepository,
        IUnitOfWork unitOfWork) {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken) {
        var customerIdResult = CustomerId.From(request.CustomerId);
        if (!customerIdResult.IsSuccess)
            return Result<Guid>.Failure(customerIdResult.Error!);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Result<Guid>.Failure(DomainErrors.Customer.NotFound(request.CustomerId));

        var addressResult = Address.Create(
            request.ShippingStreet,
            request.ShippingCity,
            request.ShippingState,
            request.ShippingPostalCode,
            request.ShippingCountry);

        if (!addressResult.IsSuccess)
            return Result<Guid>.Failure(addressResult.Error!);

        var orderId = OrderId.Create();
        var orderResult = Order.Create(orderId, customerIdResult.Value!, addressResult.Value!);
        if (!orderResult.IsSuccess)
            return Result<Guid>.Failure(orderResult.Error!);

        var order = orderResult.Value!;

        foreach (var lineCommand in request.Lines) {
            var product = await _productRepository.GetByIdAsync(lineCommand.ProductId, cancellationToken);
            if (product is null)
                return Result<Guid>.Failure(DomainErrors.Product.NotFound(lineCommand.ProductId));

            var addResult = order.AddProduct(product, lineCommand.Quantity);
            if (!addResult.IsSuccess)
                return Result<Guid>.Failure(addResult.Error!);
        }

        var confirmResult = order.Confirm();
        if (!confirmResult.IsSuccess)
            return Result<Guid>.Failure(confirmResult.Error!);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(order.Id.Value);
    }
}
