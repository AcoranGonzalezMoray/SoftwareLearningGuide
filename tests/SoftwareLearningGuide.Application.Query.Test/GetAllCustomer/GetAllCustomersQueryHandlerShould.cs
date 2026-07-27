using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Query.Test.Helpers;
using SoftwareLearningGuide.Helper.Test.Builders;
using SoftwareLearningGuide.Helper.Test.Extensions;
using SoftwareLearningGuide.Helper.Test.Fixtures;
using SoftwareLearningGuide.Helper.Tests.Helper;
using SoftwareLearningGuide.Infraestructure.Data.Context;

namespace SoftwareLearningGuide.Application.Query.GetAllCustomer;

public class GetAllCustomersQueryHandlerShould : EFDatabase<ApplicationDbContext> {
    private SqlConnection _connection = null!;
    private GetAllCustomersQueryHandler _handler = null!;

    [SetUp]
    public void SetUp() {
        _connection = new SqlConnection(SqlServerTestContainer.ConnectionString);
        _handler = new GetAllCustomersQueryHandler(_connection);
    }

    [Test]
    public void Constructor_NullConnection_ThrowsArgumentNullException() {
        var act = () => new GetAllCustomersQueryHandler(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList() {
        var query = new GetAllCustomersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Customers.Should().BeEmpty();
        result.Value!.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task Handle_WithCustomers_ReturnsAll() {
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, Guid.NewGuid(), "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertCustomer(_connection, Guid.NewGuid(), "Jane", "Smith", "jane@test.com", createdAt);

        var query = new GetAllCustomersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Customers.Should().HaveCount(2);
        result.Value!.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task Handle_WithCustomers_ReturnsOrderedByCreatedAtDesc() {
        var olderTime = DateTime.UtcNow.AddHours(-2);
        var newerTime = DateTime.UtcNow;
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();

        SqlServerDbHelper.InsertCustomer(_connection, olderId, "Old", "User", "old@test.com", olderTime);
        SqlServerDbHelper.InsertCustomer(_connection, newerId, "New", "User", "new@test.com", newerTime);

        var query = new GetAllCustomersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new[] {
            new CustomerSummaryDtoBuilder().WithId(newerId).WithFirstName("New").WithLastName("User").WithEmail("new@test.com").WithCreatedAt(newerTime).Build(),
            new CustomerSummaryDtoBuilder().WithId(olderId).WithFirstName("Old").WithLastName("User").WithEmail("old@test.com").WithCreatedAt(olderTime).Build()
        };
        result.Value!.Customers.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering().WithTolerance());
    }

    [Test]
    public async Task Handle_CustomerWithoutOrders_ReturnsZeroOrderCount() {
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);

        var query = new GetAllCustomersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new CustomerSummaryDtoBuilder()
            .WithId(customerId).WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com")
            .WithCreatedAt(createdAt).WithOrderCount(0).Build();
        result.Value!.Customers.Single().Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_CustomerWithOrders_ReturnsOrderCount() {
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "John", "Doe", "john@test.com", createdAt);
        SqlServerDbHelper.InsertOrder(_connection, Guid.NewGuid(), customerId, 0,
            "123 Main St", "Springfield", "IL", "62704", "US", createdAt);

        var query = new GetAllCustomersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new CustomerSummaryDtoBuilder()
            .WithId(customerId).WithFirstName("John").WithLastName("Doe").WithEmail("john@test.com")
            .WithCreatedAt(createdAt).WithOrderCount(1).Build();
        result.Value!.Customers.Single().Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [Test]
    public async Task Handle_WithMultipleCustomers_ReturnsCorrectFields() {
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        SqlServerDbHelper.InsertCustomer(_connection, customerId, "Alice", "Johnson", "alice@test.com", createdAt);

        var query = new GetAllCustomersQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var expected = new CustomerSummaryDtoBuilder()
            .WithId(customerId).WithFirstName("Alice").WithLastName("Johnson").WithEmail("alice@test.com")
            .WithCreatedAt(createdAt).WithOrderCount(0).Build();
        result.Value!.Customers.Single().Should().BeEquivalentTo(expected, options => options.WithTolerance());
    }

    [TearDown]
    public void TearDown() {
        _connection?.Dispose();
    }
}
