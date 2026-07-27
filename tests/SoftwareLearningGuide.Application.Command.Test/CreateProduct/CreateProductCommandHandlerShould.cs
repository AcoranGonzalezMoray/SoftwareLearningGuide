using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Command.CreateProduct;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Entities;

namespace SoftwareLearningGuide.Application.Command.Test;

public class CreateProductCommandHandlerTests {
    private IProductWriteRepository _repository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _repository = Substitute.For<IProductWriteRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateProductCommandHandler(_repository, _unitOfWork);
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesProductAndReturnsId() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 999.99m,
            Currency = "USD",
            StockQuantity = 10
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_NegativePrice_ReturnsFailure() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Desc",
            Price = -10m,
            Currency = "USD",
            StockQuantity = 10
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_EmptyName_ReturnsFailure() {
        var command = new CreateProductCommand {
            Name = "",
            Description = "Desc",
            Price = 100m,
            Currency = "USD",
            StockQuantity = 10
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_EmptyDescription_ReturnsFailure() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "",
            Price = 100m,
            Currency = "USD",
            StockQuantity = 10
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_InvalidCurrency_ReturnsFailure() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Desc",
            Price = 100m,
            Currency = "US",
            StockQuantity = 10
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_EmptyCurrency_ReturnsFailure() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Desc",
            Price = 100m,
            Currency = "",
            StockQuantity = 10
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_NegativeStock_ReturnsFailure() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Desc",
            Price = 100m,
            Currency = "USD",
            StockQuantity = -5
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ZeroPrice_ReturnsFailure() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Desc",
            Price = 0m,
            Currency = "USD",
            StockQuantity = 10
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RepositoryThrows_PropagatesException() {
        _repository.When(x => x.AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()))
            .Throw(new InvalidOperationException("DB Error"));

        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Desc",
            Price = 100m,
            Currency = "USD",
            StockQuantity = 10
        };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_NullRepository_ThrowsArgumentNullException() {
        var act = () => new CreateProductCommandHandler(null!, _unitOfWork);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_NullUnitOfWork_ThrowsArgumentNullException() {
        var act = () => new CreateProductCommandHandler(_repository, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
