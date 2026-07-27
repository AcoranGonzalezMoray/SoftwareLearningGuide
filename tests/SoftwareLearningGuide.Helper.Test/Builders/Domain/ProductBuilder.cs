using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Helper.Test.Builders.Domain;

public class ProductBuilder {
    private ProductId _id = ProductId.Create();
    private string _name = "Laptop";
    private string _description = "High-performance laptop";
    private Money _price = null!;
    private int _stockQuantity = 10;

    public ProductBuilder() {
        var moneyResult = Money.Create(999.99m, "USD");
        _price = moneyResult.Value!;
    }

    public ProductBuilder WithId(ProductId id) { _id = id; return this; }
    public ProductBuilder WithName(string name) { _name = name; return this; }
    public ProductBuilder WithDescription(string description) { _description = description; return this; }
    public ProductBuilder WithPrice(Money price) { _price = price; return this; }
    public ProductBuilder WithPrice(decimal amount, string currency = "USD") {
        var result = Money.Create(amount, currency);
        _price = result.Value!;
        return this;
    }
    public ProductBuilder WithStockQuantity(int stockQuantity) { _stockQuantity = stockQuantity; return this; }

    public Result<Product> BuildResult() => Product.Create(_id, _name, _description, _price, _stockQuantity);

    public Product Build() {
        var result = BuildResult();
        if (!result.IsSuccess)
            throw new InvalidOperationException($"ProductBuilder failed: {result.Error}");
        return result.Value!;
    }
}
