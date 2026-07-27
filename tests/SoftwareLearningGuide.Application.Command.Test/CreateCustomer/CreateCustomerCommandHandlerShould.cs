using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Command.CreateCustomer;
using SoftwareLearningGuide.Application.Command.Ports;
using SoftwareLearningGuide.Core.Business.Entities;

namespace SoftwareLearningGuide.Application.Command.Test;

public class CreateCustomerCommandHandlerTests {
    private ICustomerWriteRepository _repository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateCustomerCommandHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _repository = Substitute.For<ICustomerWriteRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateCustomerCommandHandler(_repository, _unitOfWork);
    }

    [Test]
    public void Constructor_NullRepository_ThrowsArgumentNullException() {
        var act = () => new CreateCustomerCommandHandler(null!, _unitOfWork);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException() {
        var act = () => new CreateCustomerCommandHandler(_repository, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesCustomerAndReturnsId() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithAddress_SetsDefaultShippingAddress() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            DefaultStreet = "123 Main St",
            DefaultCity = "Springfield",
            DefaultState = "IL",
            DefaultPostalCode = "62704",
            DefaultCountry = "US"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<Customer>(c => c.DefaultShippingAddress != null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_InvalidEmail_ReturnsFailure() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = "not-an-email"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_EmptyEmail_ReturnsFailure() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = ""
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_EmptyFirstName_ReturnsFailure() {
        var command = new CreateCustomerCommand {
            FirstName = "",
            LastName = "Doe",
            Email = "john@example.com"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_EmptyLastName_ReturnsFailure() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "",
            Email = "john@example.com"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_InvalidAddress_ReturnsFailure() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            DefaultStreet = "123 Main St",
            DefaultCity = ""
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_InvalidAddressWithEmptyStreet_DoesNotSetAddress() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            DefaultStreet = "",
            DefaultCity = "Springfield"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<Customer>(c => c.DefaultShippingAddress == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithoutAddress_DoesNotSetDefaultAddress() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<Customer>(c => c.DefaultShippingAddress == null),
            Arg.Any<CancellationToken>());
    }
}
