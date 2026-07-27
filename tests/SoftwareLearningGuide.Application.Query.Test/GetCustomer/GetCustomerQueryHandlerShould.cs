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

namespace SoftwareLearningGuide.Application.Query.GetCustomer;

public class GetCustomerQueryHandlerShould : EFDatabase<ApplicationDbContext> {
    private SqlConnection _connection = null!;
    private GetCustomerQueryHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _connection = new SqlConnection(SqlServerTestContainer.ConnectionString);
        _handler = new GetCustomerQueryHandler(_connection);
    }

    [Test]
    public void Constructor_NullConnection_ThrowsArgumentNullException() {
        var act = () => new GetCustomerQueryHandler(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_ExistingCustomer_ReturnsSuccess() {
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);

        var query = new GetCustomerQuery(customerId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new CustomerDtoBuilder()
            .WithId(customerId).WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com")
            .WithCreatedAt(createdAt).WithOrderCount(0).WithoutDefaultAddress().Build();
        result.Value!.Customer.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_NonExistingCustomer_ReturnsFailure() {
        var customerId = Guid.NewGuid();
        var query = new GetCustomerQuery(customerId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Customer.NotFound(customerId.ToString()));
    }

    [Test]
    public async Task Handle_CustomerWithoutOrders_ReturnsZeroOrderCount() {
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);

        var query = new GetCustomerQuery(customerId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new CustomerDtoBuilder()
            .WithId(customerId).WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com")
            .WithOrderCount(0).WithoutDefaultAddress().Build();
        result.Value!.Customer.Should().BeEquivalentTo(expected, options => options.Excluding(c => c.CreatedAt).WithTolerance());
    }

    [Test]
    public async Task Handle_CustomerWithOrders_ReturnsOrderCount() {
        var customerId = Guid.NewGuid();
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId1, customerId, 0,
            "123 Main St", "Springfield", "IL", "62704", "US", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, orderId2, customerId, 1,
            "456 Oak Ave", "Chicago", "IL", "60601", "US", createdAt);

        var query = new GetCustomerQuery(customerId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new CustomerDtoBuilder()
            .WithId(customerId).WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com")
            .WithOrderCount(2).WithoutDefaultAddress().Build();
        result.Value!.Customer.Should().BeEquivalentTo(expected, options => options.Excluding(c => c.CreatedAt).WithTolerance());
    }

    [Test]
    public async Task Handle_CustomerWithAddress_ReturnsAddressFields() {
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.UpdateCustomer(_connection, customerId);

        var query = new GetCustomerQuery(customerId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new CustomerDtoBuilder()
            .WithId(customerId).WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com")
            .WithDefaultAddress("123 Main St", "Springfield", "IL", "62704", "US")
            .WithCreatedAt(createdAt).WithOrderCount(0).Build();
        result.Value!.Customer.Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_CustomerWithoutAddress_ReturnsNullAddressFields() {
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);

        var query = new GetCustomerQuery(customerId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new CustomerDtoBuilder()
            .WithId(customerId).WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com")
            .WithoutDefaultAddress().Build();
        result.Value!.Customer.Should().BeEquivalentTo(expected, options => options.Excluding(c => c.CreatedAt).WithTolerance());
    }

    [TearDown]
    public void TearDown() {
        _connection?.Dispose();
    }
}
