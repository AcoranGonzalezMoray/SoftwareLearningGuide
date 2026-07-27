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

namespace SoftwareLearningGuide.Application.Query.GetOrder;

public class GetOrderQueryHandlerShould : EFDatabase<ApplicationDbContext> {
    private SqlConnection _connection = null!;
    private GetOrderQueryHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _connection = new SqlConnection(SqlServerTestContainer.ConnectionString);
        _handler = new GetOrderQueryHandler(_connection);
    }

    [Test]
    public void Constructor_NullConnection_ThrowsArgumentNullException() {
        var act = () => new GetOrderQueryHandler(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_ExistingOrder_ReturnsSuccess() {
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId, customerId, 0,
            "123 Main St", "Springfield", "IL", "62704", "US", createdAt);
        SqlServerDbHelper.InsertProduct(_connection, productId, "Laptop", "Gaming", 100m, "USD", 10, createdAt);
        SqlServerDbHelper.InsertOrderLine(_connection, orderLineId, orderId, productId, "Laptop", 100m, "USD", 2, createdAt);

        var query = new GetOrder.GetOrderQuery(orderId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expectedLine = new OrderLineDtoBuilder()
            .WithId(orderLineId).WithProductId(productId).WithProductName("Laptop")
            .WithUnitPrice(100m).WithCurrency("USD").WithQuantity(2).Build();
        var expected = new OrderDtoBuilder()
            .WithId(orderId).WithCustomerId(customerId).WithCustomerName("John Doe")
            .WithCustomerEmail("john@test.com").WithStatus("0").WithCurrency("USD")
            .WithShippingAddress("123 Main St", "Springfield", "IL", "62704", "US")
            .WithCreatedAt(createdAt).WithTotalAmount(200m).WithTotalItems(2).WithLineCount(1)
            .WithLine(expectedLine).Build();
        result.Value!.Order.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_ExistingOrder_ReturnsShippingAddress() {
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId, customerId, 0,
            "456 Oak Ave", "Chicago", "IL", "60601", "US", createdAt);

        var query = new GetOrder.GetOrderQuery(orderId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new OrderDtoBuilder()
            .WithId(orderId).WithCustomerId(customerId).WithCustomerName("John Doe")
            .WithCustomerEmail("john@test.com").WithStatus("0")
            .WithShippingAddress("456 Oak Ave", "Chicago", "IL", "60601", "US")
            .WithCreatedAt(createdAt).WithTotalAmount(0m).WithTotalItems(0).WithLineCount(0)
            .Build();
        result.Value!.Order.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_NonExistingOrder_ReturnsFailure() {
        var orderId = Guid.NewGuid();
        var query = new GetOrder.GetOrderQuery(orderId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Order.NotFound(orderId));
    }

    [Test]
    public async Task Handle_OrderWithMultipleLines_ReturnsCorrectTotals() {
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var lineId1 = Guid.NewGuid();
        var lineId2 = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId, customerId, 0,
            "123 Main St", "Springfield", "IL", "62704", "US", createdAt);
        SqlServerDbHelper.InsertProduct(_connection, productId1, "Laptop", "Desc", 100m, "USD", 10, createdAt);
        SqlServerDbHelper.InsertProduct(_connection, productId2, "Mouse", "Desc", 25m, "USD", 50, createdAt);
        SqlServerDbHelper.InsertOrderLine(_connection, lineId1, orderId, productId1, "Laptop", 100m, "USD", 2, createdAt);
        SqlServerDbHelper.InsertOrderLine(_connection, lineId2, orderId, productId2, "Mouse", 25m, "USD", 4, createdAt);

        var query = new GetOrder.GetOrderQuery(orderId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expectedLine1 = new OrderLineDtoBuilder()
            .WithId(lineId1).WithProductId(productId1).WithProductName("Laptop")
            .WithUnitPrice(100m).WithCurrency("USD").WithQuantity(2).Build();
        var expectedLine2 = new OrderLineDtoBuilder()
            .WithId(lineId2).WithProductId(productId2).WithProductName("Mouse")
            .WithUnitPrice(25m).WithCurrency("USD").WithQuantity(4).Build();
        var expected = new OrderDtoBuilder()
            .WithId(orderId).WithCustomerId(customerId).WithCustomerName("John Doe")
            .WithCustomerEmail("john@test.com").WithStatus("0").WithCurrency("USD")
            .WithShippingAddress("123 Main St", "Springfield", "IL", "62704", "US")
            .WithCreatedAt(createdAt).WithTotalAmount(300m).WithTotalItems(6).WithLineCount(2)
            .WithLines([expectedLine1, expectedLine2]).Build();
        result.Value!.Order.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_OrderWithNoLines_ReturnsZeroTotals() {
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId, customerId, 0,
            "123 Main St", "Springfield", "IL", "62704", "US", createdAt);

        var query = new GetOrder.GetOrderQuery(orderId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new OrderDtoBuilder()
            .WithId(orderId).WithCustomerId(customerId).WithCustomerName("John Doe")
            .WithCustomerEmail("john@test.com").WithStatus("0")
            .WithShippingAddress("123 Main St", "Springfield", "IL", "62704", "US")
            .WithCreatedAt(createdAt).WithTotalAmount(0m).WithTotalItems(0).WithLineCount(0)
            .Build();
        result.Value!.Order.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_OrderWithDifferentCurrencyLines_ReturnsCurrency() {
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId, customerId, 0,
            "123 Main St", "Springfield", "IL", "62704", "US", createdAt);
        SqlServerDbHelper.InsertProduct(_connection, productId, "Book", "Desc", 10m, "EUR", 100, createdAt);
        SqlServerDbHelper.InsertOrderLine(_connection, lineId, orderId, productId, "Book", 10m, "EUR", 3, createdAt);

        var query = new GetOrder.GetOrderQuery(orderId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expectedLine = new OrderLineDtoBuilder()
            .WithId(lineId).WithProductId(productId).WithProductName("Book")
            .WithUnitPrice(10m).WithCurrency("EUR").WithQuantity(3).Build();
        var expected = new OrderDtoBuilder()
            .WithId(orderId).WithCustomerId(customerId).WithCustomerName("John Doe")
            .WithCustomerEmail("john@test.com").WithStatus("0").WithCurrency("EUR")
            .WithShippingAddress("123 Main St", "Springfield", "IL", "62704", "US")
            .WithCreatedAt(createdAt).WithTotalAmount(30m).WithTotalItems(3).WithLineCount(1)
            .WithLine(expectedLine).Build();
        result.Value!.Order.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [TearDown]
    public void TearDown() {
        _connection?.Dispose();
    }
}
