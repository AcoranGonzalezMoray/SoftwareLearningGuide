using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Respawn;
using SoftwareLearningGuide.Helper.Test.Fixtures;
using SoftwareLearningGuide.Infraestructure.Data.Context;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SoftwareLearningGuide.Api.Test.E2E;

public abstract class E2ETestBase : IDisposable {
    protected readonly HttpClient Client;
    protected readonly string ConnectionString;
    protected readonly JsonSerializerOptions JsonOptions;
    private readonly WebApplicationFactory<Program> _factory;

    protected E2ETestBase() {
        ConnectionString = SqlServerTestContainer.ConnectionString;
        JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.ConfigureAppConfiguration((context, config) => {
                    config.AddInMemoryCollection(new Dictionary<string, string?> {
                        ["ConnectionStrings:SoftwareLearningGuide"] = ConnectionString,
                        ["FeatureManagement:FT_APPLY_MIGRATIONS_ON_START"] = "false",
                        ["FeatureManagement:FT_ENABLE_ORDER_CONTROLLER"] = "true",
                        ["FeatureManagement:FT_ENABLE_ORDER_CREATION"] = "true",
                        ["FeatureManagement:FT_ENABLE_ORDER_RETRIEVAL"] = "true",
                        ["FeatureManagement:FT_ENABLE_ORDER_LIST"] = "true",
                        ["FeatureManagement:FT_ENABLE_PRODUCT_CONTROLLER"] = "true",
                        ["FeatureManagement:FT_ENABLE_PRODUCT_CREATION"] = "true",
                        ["FeatureManagement:FT_ENABLE_PRODUCT_RETRIEVAL"] = "true",
                        ["FeatureManagement:FT_ENABLE_PRODUCT_LIST"] = "true",
                        ["FeatureManagement:FT_ENABLE_CUSTOMER_CONTROLLER"] = "true",
                        ["FeatureManagement:FT_ENABLE_CUSTOMER_CREATION"] = "true",
                        ["FeatureManagement:FT_ENABLE_CUSTOMER_RETRIEVAL"] = "true",
                        ["FeatureManagement:FT_ENABLE_CUSTOMER_LIST"] = "true",
                    });
                });

                builder.ConfigureServices(services => {
                    services.AddScoped<IDbConnection>(_ => new SqlConnection(ConnectionString));
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(ConnectionString));
                });


            });

        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost/"),
        });
    }

    [SetUp]
    public async Task ResetDatabase() {
        var connectionString = SqlServerTestContainer.ConnectionString;
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var context = (ApplicationDbContext)Activator.CreateInstance(typeof(ApplicationDbContext), optionsBuilder.Options)!;
        context.Database.Migrate();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions {
            TablesToIgnore = ["__EFMigrationsHistory"],
            SchemasToExclude = [],
            DbAdapter = DbAdapter.SqlServer
        });

        await respawner.ResetAsync(connection);
        await connection.CloseAsync();
    }

    protected static void InsertCustomer(SqlConnection connection, Guid id, string firstName,
        string lastName, string email, DateTime createdAt) {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Customers (Id, FirstName, LastName, Email, CreatedAt)
            VALUES (@Id, @FirstName, @LastName, @Email, @CreatedAt)
            """;
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@FirstName", firstName);
        cmd.Parameters.AddWithValue("@LastName", lastName);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
        cmd.ExecuteNonQuery();
        connection.Close();
    }

    protected static void InsertProduct(SqlConnection connection, Guid id, string name,
        string description, decimal price, string currency, int stockQuantity, DateTime createdAt) {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Products (Id, Name, Description, Price, PriceCurrency, StockQuantity, CreatedAt)
            VALUES (@Id, @Name, @Description, @Price, @Currency, @Stock, @CreatedAt)
            """;
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Name", name);
        cmd.Parameters.AddWithValue("@Description", description);
        cmd.Parameters.AddWithValue("@Price", price);
        cmd.Parameters.AddWithValue("@Currency", currency);
        cmd.Parameters.AddWithValue("@Stock", stockQuantity);
        cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
        cmd.ExecuteNonQuery();
        connection.Close();
    }

    protected static void InsertOrder(SqlConnection connection, Guid id, Guid customerId, int status,
        string street, string city, string state, string postalCode, string country, DateTime createdAt) {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Orders (Id, CustomerId, Status, ShippingStreet, ShippingCity, ShippingState,
                ShippingPostalCode, ShippingCountry, CreatedAt)
            VALUES (@Id, @CustomerId, @Status, @Street, @City, @State, @PostalCode, @Country, @CreatedAt)
            """;
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@CustomerId", customerId);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@Street", street);
        cmd.Parameters.AddWithValue("@City", city);
        cmd.Parameters.AddWithValue("@State", state);
        cmd.Parameters.AddWithValue("@PostalCode", postalCode);
        cmd.Parameters.AddWithValue("@Country", country);
        cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
        cmd.ExecuteNonQuery();
        connection.Close();
    }

    protected static void InsertOrderLine(SqlConnection connection, Guid id, Guid orderId, Guid productId,
        string productName, decimal unitPrice, string currency, int quantity, DateTime createdAt) {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO OrderLines (Id, OrderId, ProductId, ProductName, UnitPrice, Currency, Quantity, CreatedAt)
            VALUES (@Id, @OrderId, @ProductId, @ProductName, @UnitPrice, @Currency, @Quantity, @CreatedAt)
            """;
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@ProductId", productId);
        cmd.Parameters.AddWithValue("@ProductName", productName);
        cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
        cmd.Parameters.AddWithValue("@Currency", currency);
        cmd.Parameters.AddWithValue("@Quantity", quantity);
        cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
        cmd.ExecuteNonQuery();
        connection.Close();
    }

    protected static void UpdateCustomerAddress(SqlConnection connection, Guid customerId,
        string street, string city, string state, string postalCode, string country) {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE Customers SET DefaultStreet=@Street, DefaultCity=@City, DefaultState=@State,
                DefaultPostalCode=@PostalCode, DefaultCountry=@Country WHERE Id=@Id
            """;
        cmd.Parameters.AddWithValue("@Id", customerId.ToString());
        cmd.Parameters.AddWithValue("@Street", street);
        cmd.Parameters.AddWithValue("@City", city);
        cmd.Parameters.AddWithValue("@State", state);
        cmd.Parameters.AddWithValue("@PostalCode", postalCode);
        cmd.Parameters.AddWithValue("@Country", country);
        cmd.ExecuteNonQuery();
        connection.Close();
    }

    protected async Task<JsonNode> GetResponseJson(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content) ?? throw new InvalidOperationException("Empty response body");
    }

    protected async Task<JsonNode> LoadFixture(string relativePath) {
        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var fixturePath = Path.Combine(baseDir, relativePath);
        var json = await File.ReadAllTextAsync(fixturePath);
        return JsonNode.Parse(json) ?? throw new InvalidOperationException($"Empty fixture file: {fixturePath}");
    }

    public void Dispose() {
        Client.Dispose();
        _factory.Dispose();
    }
}