using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using SoftwareLearningGuide.Application.Command.CreateProduct;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SoftwareLearningGuide.Api.Test.E2E.Product;

public class ProductControllerE2EShould : E2ETestBase {
    private static readonly DateTime CreatedAt = new(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Product1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Product2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Test]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyList() {
        var response = await Client.GetAsync("/api/v1/Product");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Product/ResponseFixtures/GetAll_empty.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetAll_WithProducts_ReturnsProducts() {
        await using var connection = new SqlConnection(ConnectionString);
        InsertProduct(connection, Product1Id, "Laptop", "Gaming laptop", 999.99m, "USD", 10, CreatedAt);
        InsertProduct(connection, Product2Id, "Mouse", "Wireless mouse", 25.50m, "USD", 50, CreatedAt);

        var response = await Client.GetAsync("/api/v1/Product");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Product/ResponseFixtures/GetAll_with_products.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetById_ExistingProduct_ReturnsProduct() {
        await using var connection = new SqlConnection(ConnectionString);
        InsertProduct(connection, Product1Id, "Laptop", "Gaming laptop", 999.99m, "USD", 10, CreatedAt);

        var response = await Client.GetAsync($"/api/v1/Product/{Product1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actual = await GetResponseJson(response);
        var expected = await LoadFixture("E2E/Product/ResponseFixtures/GetById_existing.json");
        JsonNode.DeepEquals(actual, expected).Should().BeTrue();
    }

    [Test]
    public async Task GetById_NonExistingProduct_ReturnsNotFound() {
        var response = await Client.GetAsync($"/api/v1/Product/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_ValidProduct_ReturnsCreated() {
        var command = new CreateProductCommand {
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 999.99m,
            Currency = "USD",
            StockQuantity = 10
        };
        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/v1/Product", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/Product/");
        var body = await response.Content.ReadAsStringAsync();
        var bodyNode = JsonNode.Parse(body)!;
        bodyNode["productId"]!.GetValue<Guid>().Should().NotBeEmpty();
    }
}