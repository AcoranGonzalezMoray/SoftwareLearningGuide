using SoftwareLearningGuide.Application.Query.GetProduct;

namespace SoftwareLearningGuide.Helper.Test.Builders;

public class ProductDtoBuilder {
    private Guid _id = Guid.NewGuid();
    private string _name = "Laptop";
    private string _description = "High-performance laptop";
    private decimal _price = 999.99m;
    private string _currency = "USD";
    private int _stockQuantity = 10;
    private DateTime _createdAt = new(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private DateTime? _updatedAt;

    public ProductDtoBuilder WithId(Guid id) { _id = id; return this; }
    public ProductDtoBuilder WithName(string name) { _name = name; return this; }
    public ProductDtoBuilder WithDescription(string description) { _description = description; return this; }
    public ProductDtoBuilder WithPrice(decimal price) { _price = price; return this; }
    public ProductDtoBuilder WithCurrency(string currency) { _currency = currency; return this; }
    public ProductDtoBuilder WithStockQuantity(int stockQuantity) { _stockQuantity = stockQuantity; return this; }
    public ProductDtoBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }
    public ProductDtoBuilder WithUpdatedAt(DateTime? updatedAt) { _updatedAt = updatedAt; return this; }

    public ProductDto Build() => new() {
        Id = _id,
        Name = _name,
        Description = _description,
        Price = _price,
        Currency = _currency,
        StockQuantity = _stockQuantity,
        CreatedAt = _createdAt,
        UpdatedAt = _updatedAt
    };
}
