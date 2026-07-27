using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Helper.Test.Builders.Domain;

public class OrderLineBuilder {
    private OrderLineId _id = OrderLineId.Create();
    private ProductId _productId = ProductId.Create();
    private string _productName = "Laptop";
    private Money _unitPrice = null!;
    private int _quantity = 2;

    public OrderLineBuilder() {
        var moneyResult = Money.Create(100m, "USD");
        _unitPrice = moneyResult.Value!;
    }

    public OrderLineBuilder WithId(OrderLineId id) { _id = id; return this; }
    public OrderLineBuilder WithProductId(ProductId productId) { _productId = productId; return this; }
    public OrderLineBuilder WithProductName(string productName) { _productName = productName; return this; }
    public OrderLineBuilder WithUnitPrice(Money unitPrice) { _unitPrice = unitPrice; return this; }
    public OrderLineBuilder WithUnitPrice(decimal amount, string currency = "USD") {
        var result = Money.Create(amount, currency);
        _unitPrice = result.Value!;
        return this;
    }
    public OrderLineBuilder WithQuantity(int quantity) { _quantity = quantity; return this; }

    public Result<OrderLine> BuildResult() => OrderLine.Create(_id, _productId, _productName, _unitPrice, _quantity);

    public OrderLine Build() {
        var result = BuildResult();
        if (!result.IsSuccess)
            throw new InvalidOperationException($"OrderLineBuilder failed: {result.Error}");
        return result.Value!;
    }
}
