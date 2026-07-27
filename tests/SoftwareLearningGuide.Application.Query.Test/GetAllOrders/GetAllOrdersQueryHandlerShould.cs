using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Query.Test.Helpers;
using SoftwareLearningGuide.Helper.Test.Builders;
using SoftwareLearningGuide.Helper.Test.Extensions;
using SoftwareLearningGuide.Helper.Test.Fixtures;
using SoftwareLearningGuide.Helper.Tests.Helper;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Application.Query.GetAllOrders;

public class GetAllOrdersQueryHandlerShould : EFDatabase<ApplicationDbContext> {
    private SqlConnection _connection = null!;
    private GetAllOrdersQueryHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _connection = new SqlConnection(SqlServerTestContainer.ConnectionString);
        _handler = new GetAllOrdersQueryHandler(_connection);
    }

    [Test]
    public void Constructor_NullConnection_ThrowsArgumentNullException() {
        var act = () => new GetAllOrdersQueryHandler(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList() {
        var query = new GetAllOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Orders.Should().BeEmpty();
        result.Value!.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task Handle_WithOrders_ReturnsAll() {
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, Guid.NewGuid(), customerId, 0, "123 Main St", "Springfield", "IL", "62704", "US", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, Guid.NewGuid(), customerId, 1, "456 Oak Ave", "Chicago", "IL", "60601", "US", createdAt);

        var query = new GetAllOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Orders.Should().HaveCount(2);
        result.Value!.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task Handle_WithOrders_ReturnsCustomerName() {
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "Jane", "Smith", "jane@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId, customerId, 0, "123 Main St", "Springfield", "IL", "62704", "US", createdAt);

        var query = new GetAllOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new OrderSummaryDtoBuilder()
            .WithId(orderId).WithCustomerId(customerId).WithCustomerName("Jane Smith")
            .WithStatus("0").WithCreatedAt(createdAt).Build();
        result.Value!.Orders.Single().Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_OrderWithLines_ReturnsLineCount() {
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId, customerId, 0, "123 Main St", "Springfield", "IL", "62704", "US", createdAt);
        SqlServerDbHelper.InsertProduct(_connection, productId, "Laptop", "Desc", 100m, "USD", 10, createdAt);
        SqlServerDbHelper.InsertOrderLine(_connection, Guid.NewGuid(), orderId, productId, "Laptop", 100m, "USD", 2, createdAt);

        var query = new GetAllOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new OrderSummaryDtoBuilder()
            .WithId(orderId).WithCustomerId(customerId).WithCustomerName("John Doe")
            .WithStatus("0").WithLineCount(1).WithCreatedAt(createdAt).Build();
        result.Value!.Orders.Single().Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_OrderWithoutLines_ReturnsZeroLineCount() {
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId, customerId, 0, "123 Main St", "Springfield", "IL", "62704", "US", createdAt);

        var query = new GetAllOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new OrderSummaryDtoBuilder()
            .WithId(orderId).WithCustomerId(customerId).WithCustomerName("John Doe")
            .WithStatus("0").WithLineCount(0).WithCreatedAt(createdAt).Build();
        result.Value!.Orders.Single().Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_MultipleOrdersWithDifferentLineCounts_ReturnsCorrectCounts() {
        var customerId = Guid.NewGuid();
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId1, customerId, 0, "123 Main St", "Springfield", "IL", "62704", "US", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId2, customerId, 0, "456 Oak Ave", "Chicago", "IL", "60601", "US", createdAt);
        SqlServerDbHelper.InsertProduct(_connection, productId, "Laptop", "Desc", 100m, "USD", 10, createdAt);
        SqlServerDbHelper.InsertOrderLine(_connection, Guid.NewGuid(), orderId1, productId, "Laptop", 100m, "USD", 2, createdAt);

        var query = new GetAllOrdersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Orders.Should().HaveCount(2);
        result.Value!.Orders.Single(o => o.Id == orderId1).LineCount.Should().Be(1);
        result.Value!.Orders.Single(o => o.Id == orderId2).LineCount.Should().Be(0);
    }

    [TearDown]
    public void TearDown() {
        _connection?.Dispose();
    }
}
