using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SoftwareLearningGuide.Api.Controllers.ProductControllerExample;
using SoftwareLearningGuide.Application.Command.CreateProduct;
using SoftwareLearningGuide.Application.Query.GetAllProducts;
using SoftwareLearningGuide.Application.Query.GetProduct;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Helper.Test.Builders;

namespace SoftwareLearningGuide.Api.Test;

public class ProductControllerShould {
    private IMediator _mediator = null!;
    private ILogger<ProductController> _logger = null!;
    private ProductController _controller = null!;

    [SetUp]
    public void SetUp() {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<ProductController>>();
        _controller = new ProductController(_mediator, _logger);
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Test]
    public void Constructor_NullMediator_ThrowsArgumentNullException() {
        var act = () => new ProductController(null!, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException() {
        var act = () => new ProductController(_mediator, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task GetAll_ReturnsOkWithProducts() {
        var response = new GetAllProductsQueryResponse {
            Products = new List<ProductDto> {
                new ProductDtoBuilder().WithName("Laptop").WithPrice(999m).WithCurrency("USD").WithStockQuantity(10).Build()
            },
            TotalCount = 1
        };
        _mediator.Send(Arg.Any<GetAllProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetAllProductsQueryResponse>.Success(response));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task GetAll_ErrorReturnsBadRequest() {
        _mediator.Send(Arg.Any<GetAllProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetAllProductsQueryResponse>.Failure("DB Error"));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GetById_ExistingProduct_ReturnsOk() {
        var productId = Guid.NewGuid();
        var response = new GetProductQueryResponse {
            Product = new ProductDtoBuilder().WithId(productId).WithName("Laptop").WithPrice(999m).WithCurrency("USD").WithStockQuantity(10).Build()
        };
        _mediator.Send(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetProductQueryResponse>.Success(response));

        var result = await _controller.GetById(productId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetById_NonExistingProduct_ReturnsNotFound() {
        _mediator.Send(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<GetProductQueryResponse>.Failure("Product not found"));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Create_ValidCommand_ReturnsCreatedAtAction() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 999.99m,
            Currency = "USD",
            StockQuantity = 10
        };
        _mediator.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
    }

    [Test]
    public async Task Create_InvalidCommand_ReturnsBadRequest() {
        var command = new CreateProductCommand { Name = "", Price = -1 };
        _mediator.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Validation error"));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
