using MediatR;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Application.Command.CreateProduct;

/// <summary>
/// Handler de MediatR para CreateProductCommand.
/// Valida reglas de negocio del dominio antes de persistir a traves del repository.
/// Usa IUnitOfWork para garantizar que toda la operación se persista en una única transacción.
/// </summary>
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>> {
    private readonly IProductWriteRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductWriteRepository repository, IUnitOfWork unitOfWork) {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken) {
        var productId = ProductId.Create();

        var priceResult = Money.Create(request.Price, request.Currency);
        if (!priceResult.IsSuccess)
            return Result<Guid>.Failure(priceResult.Error!);

        var productResult = Product.Create(
            productId,
            request.Name,
            request.Description,
            priceResult.Value!,
            request.StockQuantity);

        if (!productResult.IsSuccess)
            return Result<Guid>.Failure(productResult.Error!);

        await _repository.AddAsync(productResult.Value!, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(productId.Value);
    }
}
