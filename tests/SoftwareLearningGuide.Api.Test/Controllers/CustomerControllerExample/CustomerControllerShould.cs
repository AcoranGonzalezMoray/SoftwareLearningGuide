using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SoftwareLearningGuide.Api.Controllers.CustomerControllerExample;
using SoftwareLearningGuide.Application.Command.CreateCustomer;
using SoftwareLearningGuide.Application.Query.GetAllCustomer;
using SoftwareLearningGuide.Application.Query.GetCustomer;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Helper.Test.Builders;

namespace SoftwareLearningGuide.Api.Test;

public class CustomerControllerShould {
    private IMediator _mediator = null!;
    private ILogger<CustomerController> _logger = null!;
    private CustomerController _controller = null!;

    [SetUp]
    public void SetUp() {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<CustomerController>>();
        _controller = new CustomerController(_mediator, _logger);
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Test]
    public void Constructor_NullMediator_ThrowsArgumentNullException() {
        var act = () => new CustomerController(null!, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException() {
        var act = () => new CustomerController(_mediator, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task GetAll_ReturnsOkWithCustomers() {
        var response = new GetAllCustomersQueryResponse {
            Customers = new List<CustomerSummaryDto> {
                new CustomerSummaryDtoBuilder().WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com").Build()
            },
            TotalCount = 1
        };
        _mediator.Send(Arg.Any<GetAllCustomersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetAllCustomersQueryResponse>.Success(response));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task GetAll_ErrorReturnsBadRequest() {
        _mediator.Send(Arg.Any<GetAllCustomersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetAllCustomersQueryResponse>.Failure("Database error"));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GetById_ExistingCustomer_ReturnsOk() {
        var customerId = Guid.NewGuid();
        var response = new GetCustomerQueryResponse {
            Customer = new CustomerDtoBuilder().WithId(customerId).WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com").Build()
        };
        _mediator.Send(Arg.Any<GetCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetCustomerQueryResponse>.Success(response));

        var result = await _controller.GetById(customerId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetById_NonExistingCustomer_ReturnsNotFound() {
        _mediator.Send(Arg.Any<GetCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetCustomerQueryResponse>.Failure("Customer not found"));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Create_ValidCommand_ReturnsCreatedAtAction() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        _mediator.Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
    }

    [Test]
    public async Task Create_InvalidCommand_ReturnsBadRequest() {
        var command = new CreateCustomerCommand { FirstName = "", LastName = "", Email = "invalid" };
        _mediator.Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Validation error"));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
