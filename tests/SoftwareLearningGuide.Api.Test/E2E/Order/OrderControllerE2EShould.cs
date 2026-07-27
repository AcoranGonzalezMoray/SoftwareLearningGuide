using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Command.CreateOrder;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SoftwareLearningGuide.Api.Test.E2E.Order;

public class OrderControllerE2EShould : E2ETestBase {
    private static readonly DateTime CreatedAt = new(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Customer1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Product1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Order1Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Order2Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid OrderLine1Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Test]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyList() {
        var response = await Client.GetAsync("/api/v1/Order");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Order/ResponseFixtures/GetAll_empty.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetAll_WithOrders_ReturnsOrders() {
        await using var connection = new SqlConnection(ConnectionString);
        InsertCustomer(connection, Customer1Id, "John", "Doe", "john@test.com", CreatedAt);
        InsertOrder(connection, Order1Id, Customer1Id, 0, "123 Main St", "Springfield", "IL", "62704", "US", CreatedAt);
        InsertOrder(connection, Order2Id, Customer1Id, 0, "456 Oak Ave", "Chicago", "IL", "60601", "US", CreatedAt);

        var response = await Client.GetAsync("/api/v1/Order");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Order/ResponseFixtures/GetAll_with_orders.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetById_ExistingOrder_ReturnsOrder() {
        await using var connection = new SqlConnection(ConnectionString);
        InsertCustomer(connection, Customer1Id, "John", "Doe", "john@test.com", CreatedAt);
        InsertProduct(connection, Product1Id, "Laptop", "Gaming laptop", 999.99m, "USD", 10, CreatedAt);
        InsertOrder(connection, Order1Id, Customer1Id, 0, "123 Main St", "Springfield", "IL", "62704", "US", CreatedAt);
        InsertOrderLine(connection, OrderLine1Id, Order1Id, Product1Id, "Laptop", 999.99m, "USD", 2, CreatedAt);

        var response = await Client.GetAsync($"/api/v1/Order/{Order1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Order/ResponseFixtures/GetById_with_lines.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetById_NonExistingOrder_ReturnsNotFound() {
        var response = await Client.GetAsync($"/api/v1/Order/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_ValidOrder_ReturnsCreated() {
        await using var connection = new SqlConnection(ConnectionString);
        InsertCustomer(connection, Customer1Id, "John", "Doe", "john@test.com", CreatedAt);
        InsertProduct(connection, Product1Id, "Laptop", "Gaming laptop", 999.99m, "USD", 10, CreatedAt);

        var command = new CreateOrderCommand {
            CustomerId = Customer1Id,
            ShippingStreet = "123 Main St",
            ShippingCity = "Springfield",
            ShippingState = "IL",
            ShippingPostalCode = "62704",
            ShippingCountry = "US",
            Lines = new List<CreateOrderLineCommand> {
                new() { ProductId = Product1Id, Quantity = 2 }
            }
        };
        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/v1/Order", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/Order/");
        var body = await response.Content.ReadAsStringAsync();
        var bodyNode = JsonNode.Parse(body)!;
        bodyNode["orderId"]!.GetValue<Guid>().Should().NotBeEmpty();
    }
}