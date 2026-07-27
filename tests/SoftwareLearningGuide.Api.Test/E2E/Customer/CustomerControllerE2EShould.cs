using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Command.CreateCustomer;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SoftwareLearningGuide.Api.Test.E2E.Customer;

public class CustomerControllerE2EShould : E2ETestBase {
    private static readonly DateTime CreatedAt = new(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Customer1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Customer2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Test]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyList() {
        var response = await Client.GetAsync("/api/v1/Customer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Customer/ResponseFixtures/GetAll_empty.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetAll_WithCustomers_ReturnsCustomers() {
        await using var connection = new SqlConnection(ConnectionString);
        InsertCustomer(connection, Customer1Id, "John", "Doe", "john@test.com", CreatedAt);
        InsertCustomer(connection, Customer2Id, "Jane", "Smith", "jane@test.com", CreatedAt);

        var response = await Client.GetAsync("/api/v1/Customer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Customer/ResponseFixtures/GetAll_with_customers.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetById_ExistingCustomer_ReturnsCustomer() {
        await using var connection = new SqlConnection(ConnectionString);
        InsertCustomer(connection, Customer1Id, "John", "Doe", "john@test.com", CreatedAt);

        var response = await Client.GetAsync($"/api/v1/Customer/{Customer1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Customer/ResponseFixtures/GetById_existing.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetById_NonExistingCustomer_ReturnsNotFound() {
        var response = await Client.GetAsync($"/api/v1/Customer/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetById_CustomerWithAddress_ReturnsAddressFields() {
        await using var connection = new SqlConnection(ConnectionString);
        InsertCustomer(connection, Customer1Id, "John", "Doe", "john@test.com", CreatedAt);
        UpdateCustomerAddress(connection, Customer1Id, "123 Main St", "Springfield", "IL", "62704", "US");

        var response = await Client.GetAsync($"/api/v1/Customer/{Customer1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Customer/ResponseFixtures/GetById_with_address.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task Create_ValidCustomer_ReturnsCreated() {
        var command = new CreateCustomerCommand {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };
        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/v1/Customer", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/Customer/");
        var body = await response.Content.ReadAsStringAsync();
        var bodyNode = JsonNode.Parse(body)!;
        bodyNode["customerId"]!.GetValue<Guid>().Should().NotBeEmpty();
    }
}