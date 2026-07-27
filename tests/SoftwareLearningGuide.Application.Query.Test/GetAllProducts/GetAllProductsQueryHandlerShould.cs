using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Query.Test.Helpers;
using SoftwareLearningGuide.Helper.Test.Builders;
using SoftwareLearningGuide.Helper.Test.Extensions;
using SoftwareLearningGuide.Helper.Test.Fixtures;
using SoftwareLearningGuide.Helper.Tests.Helper;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Application.Query.GetAllProducts;

public class GetAllProductsQueryHandlerShould : EFDatabase<ApplicationDbContext> {
    private SqlConnection _connection = null!;
    private GetAllProductsQueryHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _connection = new SqlConnection(SqlServerTestContainer.ConnectionString);
        _handler = new GetAllProductsQueryHandler(_connection);
    }

    [Test]
    public void Constructor_NullConnection_ThrowsArgumentNullException() {
        var act = () => new GetAllProductsQueryHandler(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList() {
        var query = new GetAllProductsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Products.Should().BeEmpty();
        result.Value!.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task Handle_WithProducts_ReturnsAll() {
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertProduct(_connection, Guid.NewGuid(), "Laptop", "Gaming", 999m, "USD", 10, createdAt);
        SqlServerDbHelper.InsertProduct(_connection, Guid.NewGuid(), "Mouse", "Wireless", 25m, "USD", 50, createdAt);

        var query = new GetAllProductsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Products.Should().HaveCount(2);
        result.Value!.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task Handle_WithProducts_ReturnsOrderedByCreatedAtDesc() {
        var olderTime = DateTime.UtcNow.AddHours(-2);
        var newerTime = DateTime.UtcNow;
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();

        SqlServerDbHelper.InsertProduct(_connection, olderId, "Old", "Desc", 10m, "USD", 5, olderTime);
        SqlServerDbHelper.InsertProduct(_connection, newerId, "New", "Desc", 20m, "USD", 5, newerTime);

        var query = new GetAllProductsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new[] {
            new ProductDtoBuilder().WithId(newerId).WithName("New").WithDescription("Desc").WithPrice(20m)
                .WithCurrency("USD").WithStockQuantity(5).WithCreatedAt(newerTime).Build(),
            new ProductDtoBuilder().WithId(olderId).WithName("Old").WithDescription("Desc").WithPrice(10m)
                .WithCurrency("USD").WithStockQuantity(5).WithCreatedAt(olderTime).Build()
        };
        result.Value!.Products.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering().WithTolerance());
    }

    [Test]
    public async Task Handle_SingleProduct_ReturnsCorrectFields() {
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertProduct(_connection, productId, "Tablet", "Touchscreen", 299m, "EUR", 25, createdAt);

        var query = new GetAllProductsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new ProductDtoBuilder()
            .WithId(productId).WithName("Tablet").WithDescription("Touchscreen")
            .WithPrice(299m).WithCurrency("EUR").WithStockQuantity(25).WithCreatedAt(createdAt).Build();
        result.Value!.Products.Single().Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [TearDown]
    public void TearDown() {
        _connection?.Dispose();
    }
}
