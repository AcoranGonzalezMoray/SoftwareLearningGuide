using SoftwareLearningGuide.Application.Query.GetOrder;

namespace SoftwareLearningGuide.Helper.Test.Builders;

public class OrderLineDtoBuilder {
    private Guid _id = Guid.NewGuid();
    private Guid _productId = Guid.NewGuid();
    private string _productName = "Laptop";
    private decimal _unitPrice = 100m;
    private string _currency = "USD";
    private int _quantity = 2;

    public OrderLineDtoBuilder WithId(Guid id) { _id = id; return this; }
    public OrderLineDtoBuilder WithProductId(Guid productId) { _productId = productId; return this; }
    public OrderLineDtoBuilder WithProductName(string productName) { _productName = productName; return this; }
    public OrderLineDtoBuilder WithUnitPrice(decimal unitPrice) { _unitPrice = unitPrice; return this; }
    public OrderLineDtoBuilder WithCurrency(string currency) { _currency = currency; return this; }
    public OrderLineDtoBuilder WithQuantity(int quantity) { _quantity = quantity; return this; }

    public OrderLineDto Build() => new() {
        Id = _id,
        ProductId = _productId,
        ProductName = _productName,
        UnitPrice = _unitPrice,
        Currency = _currency,
        Quantity = _quantity,
        Subtotal = _unitPrice * _quantity
    };
}
