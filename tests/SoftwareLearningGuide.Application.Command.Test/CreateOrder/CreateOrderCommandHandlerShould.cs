using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Command.CreateOrder;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Aggregates;
using SoftwareLearningGuide.Core.Business.ValueObjects;
using SoftwareLearningGuide.Helper.Test.Builders.Domain;

namespace SoftwareLearningGuide.Application.Command.Test;

public class CreateOrderCommandHandlerTests {
    private IOrderWriteRepository _orderRepository = null!;
    private ICustomerWriteRepository _customerRepository = null!;
    private IProductWriteRepository _productRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateOrderCommandHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _orderRepository = Substitute.For<IOrderWriteRepository>();
        _customerRepository = Substitute.For<ICustomerWriteRepository>();
        _productRepository = Substitute.For<IProductWriteRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateOrderCommandHandler(
            _orderRepository, _customerRepository, _productRepository, _unitOfWork);
    }

    [Test]
    public void Constructor_NullOrderRepository_ThrowsArgumentNullException() {
        var act = () => new CreateOrderCommandHandler(null!, _customerRepository, _productRepository, _unitOfWork);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullCustomerRepository_ThrowsArgumentNullException() {
        var act = () => new CreateOrderCommandHandler(_orderRepository, null!, _productRepository, _unitOfWork);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullProductRepository_ThrowsArgumentNullException() {
        var act = () => new CreateOrderCommandHandler(_orderRepository, _customerRepository, null!, _unitOfWork);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException() {
        var act = () => new CreateOrderCommandHandler(_orderRepository, _customerRepository, _productRepository, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesOrderAndReturnsId() {
        var customerId = CustomerId.Create();
        var productId = ProductId.Create();
        var customer = new CustomerBuilder().WithId(customerId).Build();
        var product = new ProductBuilder().WithId(productId).Build();

        _customerRepository.GetByIdAsync(customerId.Value, Arg.Any<CancellationToken>())
            .Returns(customer);
        _productRepository.GetByIdAsync(productId.Value, Arg.Any<CancellationToken>())
            .Returns(product);

        var command = new CreateOrderCommand {
            CustomerId = customerId.Value,
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand> {
                new() { ProductId = productId.Value, Quantity = 2 }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_CustomerNotFound_ReturnsFailure() {
        _customerRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new CreateOrderCommand {
            CustomerId = Guid.NewGuid(),
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand>()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cliente");
    }

    [Test]
    public async Task Handle_ProductNotFound_ReturnsFailure() {
        var customerId = CustomerId.Create();
        var customer = new CustomerBuilder().WithId(customerId).Build();

        _customerRepository.GetByIdAsync(customerId.Value, Arg.Any<CancellationToken>())
            .Returns(customer);
        _productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var command = new CreateOrderCommand {
            CustomerId = customerId.Value,
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand> {
                new() { ProductId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Producto");
    }

    [Test]
    public async Task Handle_InvalidAddress_ReturnsFailure() {
        var customerId = CustomerId.Create();
        var customer = new CustomerBuilder().WithId(customerId).Build();

        _customerRepository.GetByIdAsync(customerId.Value, Arg.Any<CancellationToken>())
            .Returns(customer);

        var command = new CreateOrderCommand {
            CustomerId = customerId.Value,
            ShippingStreet = "",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand>()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task Handle_ProductInsufficientStock_ReturnsFailure() {
        var customerId = CustomerId.Create();
        var productId = ProductId.Create();
        var customer = new CustomerBuilder().WithId(customerId).Build();
        var product = new ProductBuilder().WithId(productId).WithStockQuantity(2).Build();

        _customerRepository.GetByIdAsync(customerId.Value, Arg.Any<CancellationToken>())
            .Returns(customer);
        _productRepository.GetByIdAsync(productId.Value, Arg.Any<CancellationToken>())
            .Returns(product);

        var command = new CreateOrderCommand {
            CustomerId = customerId.Value,
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand> {
                new() { ProductId = productId.Value, Quantity = 10 }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Stock");
    }

    [Test]
    public async Task Handle_InvalidCustomerId_ReturnsFailure() {
        var command = new CreateOrderCommand {
            CustomerId = Guid.Empty,
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand>()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task Handle_EmptyOrderLines_ReturnsFailure() {
        var customerId = CustomerId.Create();
        var customer = new CustomerBuilder().WithId(customerId).Build();

        _customerRepository.GetByIdAsync(customerId.Value, Arg.Any<CancellationToken>())
            .Returns(customer);

        var command = new CreateOrderCommand {
            CustomerId = customerId.Value,
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand>()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("vacío");
    }

    [Test]
    public async Task Handle_QuantityZero_ReturnsFailure() {
        var customerId = CustomerId.Create();
        var productId = ProductId.Create();
        var customer = new CustomerBuilder().WithId(customerId).Build();
        var product = new ProductBuilder().WithId(productId).Build();

        _customerRepository.GetByIdAsync(customerId.Value, Arg.Any<CancellationToken>())
            .Returns(customer);
        _productRepository.GetByIdAsync(productId.Value, Arg.Any<CancellationToken>())
            .Returns(product);

        var command = new CreateOrderCommand {
            CustomerId = customerId.Value,
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand> {
                new() { ProductId = productId.Value, Quantity = 0 }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task Handle_RepositoryThrows_PropagatesException() {
        var customerId = CustomerId.Create();
        var productId = ProductId.Create();
        var customer = new CustomerBuilder().WithId(customerId).Build();
        var product = new ProductBuilder().WithId(productId).Build();

        _customerRepository.GetByIdAsync(customerId.Value, Arg.Any<CancellationToken>())
            .Returns(customer);
        _productRepository.GetByIdAsync(productId.Value, Arg.Any<CancellationToken>())
            .Returns(product);
        _orderRepository.When(x => x.AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>()))
            .Throw(new InvalidOperationException("DB Error"));

        var command = new CreateOrderCommand {
            CustomerId = customerId.Value,
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand> {
                new() { ProductId = productId.Value, Quantity = 2 }
            }
        };

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
