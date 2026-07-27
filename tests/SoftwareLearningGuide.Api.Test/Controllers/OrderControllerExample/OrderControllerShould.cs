using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using NSubstitute;
using NUnit.Framework;
using SoftwareLearningGuide.Api.Controllers.OrderControllerExample;
using SoftwareLearningGuide.Api.Metrics;
using SoftwareLearningGuide.Application.Query.GetAllOrders;
using SoftwareLearningGuide.Application.Query.GetOrder;
using SoftwareLearningGuide.Application.Query.GetOrderQuery;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Helper.Test.Builders;
using System.Diagnostics.Metrics;

namespace SoftwareLearningGuide.Api.Test;

public class OrderControllerShould {
    private IMediator _mediator = null!;
    private ILogger<OrderController> _logger = null!;
    private OrderMetrics _metrics = null!;
    private IFeatureManagerSnapshot _featureManager = null!;
    private OrderController _controller = null!;

    [SetUp]
    public void SetUp() {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<OrderController>>();
        var meterFactory = Substitute.For<IMeterFactory>();
        var meter = new Meter("SoftwareLearningGuide.Orders");
        meterFactory.Create(Arg.Is<MeterOptions>(o => o.Name == "SoftwareLearningGuide.Orders")).Returns(meter);
        _metrics = new OrderMetrics(meterFactory);
        _featureManager = Substitute.For<IFeatureManagerSnapshot>();
        _featureManager.IsEnabledAsync(Arg.Any<string>()).Returns(true);
        _controller = new OrderController(_mediator, _logger, _metrics, _featureManager);
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Test]
    public void Constructor_NullMediator_ThrowsArgumentNullException() {
        var act = () => new OrderController(null!, _logger, _metrics, _featureManager);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException() {
        var act = () => new OrderController(_mediator, null!, _metrics, _featureManager);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullMetrics_ThrowsArgumentNullException() {
        var act = () => new OrderController(_mediator, _logger, null!, _featureManager);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullFeatureManager_ThrowsArgumentNullException() {
        var act = () => new OrderController(_mediator, _logger, _metrics, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task GetAll_ReturnsOkWithOrders() {
        var response = new GetAllOrdersQueryResponse {
            Orders = new List<OrderSummaryDto> {
                new OrderSummaryDtoBuilder().WithCustomerName("John Doe").WithStatus("Confirmed").Build()
            },
            TotalCount = 1
        };
        _mediator.Send(Arg.Any<GetAllOrdersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetAllOrdersQueryResponse>.Success(response));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task GetAll_ErrorReturnsBadRequest() {
        _mediator.Send(Arg.Any<GetAllOrdersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetAllOrdersQueryResponse>.Failure("Database error"));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GetById_ExistingOrder_ReturnsOk() {
        var orderId = Guid.NewGuid();
        var response = new GetOrderQueryResponse {
            Order = new OrderDtoBuilder().WithId(orderId).WithCustomerName("John Doe").Build()
        };
        _mediator.Send(Arg.Any<GetOrderQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetOrderQueryResponse>.Success(response));

        var result = await _controller.GetById(orderId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetById_NonExistingOrder_ReturnsNotFound() {
        _mediator.Send(Arg.Any<GetOrderQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetOrderQueryResponse>.Failure("Order not found"));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Create_FeatureDisabled_ReturnsNotFound() {
        _featureManager.IsEnabledAsync(Arg.Any<string>()).Returns(false);

        var command = new Application.Command.CreateOrder.CreateOrderCommand {
            CustomerId = Guid.NewGuid(),
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<Application.Command.CreateOrder.CreateOrderLineCommand>()
        };

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Create_ValidCommand_ReturnsCreatedAtAction() {
        var command = new Application.Command.CreateOrder.CreateOrderCommand {
            CustomerId = Guid.NewGuid(),
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<Application.Command.CreateOrder.CreateOrderLineCommand>()
        };
        _mediator.Send(Arg.Any<Application.Command.CreateOrder.CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
    }

    [Test]
    public async Task Create_InvalidCommand_ReturnsBadRequest() {
        var command = new Application.Command.CreateOrder.CreateOrderCommand {
            CustomerId = Guid.Empty,
            Lines = new List<Application.Command.CreateOrder.CreateOrderLineCommand>()
        };
        _mediator.Send(Arg.Any<Application.Command.CreateOrder.CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Validation error"));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
