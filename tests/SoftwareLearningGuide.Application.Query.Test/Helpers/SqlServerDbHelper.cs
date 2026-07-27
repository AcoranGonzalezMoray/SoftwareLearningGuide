using Microsoft.Data.SqlClient;

namespace SoftwareLearningGuide.Application.Query.Test.Helpers;

public static class SqlServerDbHelper {
    public static void InsertProduct(SqlConnection connection, Guid id, string name, string description,
        decimal price, string currency, int stockQuantity, DateTime createdAt) {
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

    public static void InsertCustomer(SqlConnection connection, Guid id, string firstName, string lastName,
        string email, DateTime createdAt) {
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

    public static void UpdateCustomer(SqlConnection connection, Guid id) {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Customers SET DefaultStreet='123 Main St', DefaultCity='Springfield', DefaultState='IL', DefaultPostalCode='62704', DefaultCountry='US' WHERE Id=@Id";
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
        connection.Close();
    }

    public static void InsertOrder(SqlConnection connection, Guid id, Guid customerId, int status,
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

    public static void InsertOrderLine(SqlConnection connection, Guid id, Guid orderId, Guid productId,
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
}