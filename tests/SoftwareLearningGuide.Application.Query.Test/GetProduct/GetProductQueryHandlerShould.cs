using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Query.Test.Helpers;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Helper.Test.Builders;
using SoftwareLearningGuide.Helper.Test.Extensions;
using SoftwareLearningGuide.Helper.Test.Fixtures;
using SoftwareLearningGuide.Helper.Tests.Helper;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Application.Query.GetProduct;

public class GetProductQueryHandlerShould : EFDatabase<ApplicationDbContext> {
    private SqlConnection _connection = null!;
    private GetProductQueryHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _connection = new SqlConnection(SqlServerTestContainer.ConnectionString);
        _handler = new GetProductQueryHandler(_connection);
    }

    [Test]
    public void Constructor_NullConnection_ThrowsArgumentNullException() {
        var act = () => new GetProductQueryHandler(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_ExistingProduct_ReturnsSuccess() {
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertProduct(_connection, productId, "Laptop", "Gaming", 999.99m, "USD", 10, createdAt);

        var query = new GetProductQuery(productId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new ProductDtoBuilder()
            .WithId(productId).WithName("Laptop").WithDescription("Gaming")
            .WithPrice(999.99m).WithCurrency("USD").WithStockQuantity(10)
            .WithCreatedAt(createdAt).Build();
        result.Value!.Product.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_NonExistingProduct_ReturnsFailure() {
        var productId = Guid.NewGuid();
        var query = new GetProductQuery(productId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.NotFound(productId));
    }

    [Test]
    public async Task Handle_ProductWithZeroStock_ReturnsSuccess() {
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertProduct(_connection, productId, "Phone", "Smartphone", 499m, "USD", 0, createdAt);

        var query = new GetProductQuery(productId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new ProductDtoBuilder()
            .WithId(productId).WithName("Phone").WithDescription("Smartphone")
            .WithPrice(499m).WithCurrency("USD").WithStockQuantity(0)
            .WithCreatedAt(createdAt).Build();
        result.Value!.Product.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_ProductWithDifferentCurrency_ReturnsSuccess() {
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertProduct(_connection, productId, "Book", "C# Guide", 29.99m, "EUR", 100, createdAt);

        var query = new GetProductQuery(productId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new ProductDtoBuilder()
            .WithId(productId).WithName("Book").WithDescription("C# Guide")
            .WithPrice(29.99m).WithCurrency("EUR").WithStockQuantity(100)
            .WithCreatedAt(createdAt).Build();
        result.Value!.Product.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [TearDown]
    public void TearDown() {
        _connection?.Dispose();
    }
}
