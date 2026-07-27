using SoftwareLearningGuide.Application.Query.GetOrder;

namespace SoftwareLearningGuide.Helper.Test.Builders;

public class OrderDtoBuilder {
    private Guid _id = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private string _customerName = "John Doe";
    private string _customerEmail = "john@example.com";
    private string _status = "0";
    private decimal _totalAmount;
    private string _currency = "USD";
    private int _totalItems;
    private int _lineCount;
    private string _shippingStreet = "123 Main St";
    private string _shippingCity = "Springfield";
    private string _shippingState = "IL";
    private string _shippingPostalCode = "62704";
    private string _shippingCountry = "US";
    private DateTime _createdAt = new(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private DateTime? _confirmedAt;
    private DateTime? _shippedAt;
    private DateTime? _deliveredAt;
    private DateTime? _cancelledAt;
    private List<OrderLineDto> _lines = [];

    public OrderDtoBuilder WithId(Guid id) { _id = id; return this; }
    public OrderDtoBuilder WithCustomerId(Guid customerId) { _customerId = customerId; return this; }
    public OrderDtoBuilder WithCustomerName(string customerName) { _customerName = customerName; return this; }
    public OrderDtoBuilder WithCustomerEmail(string customerEmail) { _customerEmail = customerEmail; return this; }
    public OrderDtoBuilder WithStatus(string status) { _status = status; return this; }
    public OrderDtoBuilder WithTotalAmount(decimal totalAmount) { _totalAmount = totalAmount; return this; }
    public OrderDtoBuilder WithCurrency(string currency) { _currency = currency; return this; }
    public OrderDtoBuilder WithTotalItems(int totalItems) { _totalItems = totalItems; return this; }
    public OrderDtoBuilder WithLineCount(int lineCount) { _lineCount = lineCount; return this; }
    public OrderDtoBuilder WithShippingAddress(string street, string city, string state, string postalCode, string country) {
        _shippingStreet = street; _shippingCity = city; _shippingState = state; _shippingPostalCode = postalCode; _shippingCountry = country;
        return this;
    }
    public OrderDtoBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }
    public OrderDtoBuilder WithConfirmedAt(DateTime? confirmedAt) { _confirmedAt = confirmedAt; return this; }
    public OrderDtoBuilder WithShippedAt(DateTime? shippedAt) { _shippedAt = shippedAt; return this; }
    public OrderDtoBuilder WithDeliveredAt(DateTime? deliveredAt) { _deliveredAt = deliveredAt; return this; }
    public OrderDtoBuilder WithCancelledAt(DateTime? cancelledAt) { _cancelledAt = cancelledAt; return this; }
    public OrderDtoBuilder WithLines(List<OrderLineDto> lines) { _lines = lines; return this; }
    public OrderDtoBuilder WithLine(OrderLineDto line) { _lines = [line]; return this; }

    public OrderDto Build() => new() {
        Id = _id,
        CustomerId = _customerId,
        CustomerName = _customerName,
        CustomerEmail = _customerEmail,
        Status = _status,
        TotalAmount = _totalAmount,
        Currency = _currency,
        TotalItems = _totalItems,
        LineCount = _lineCount,
        ShippingStreet = _shippingStreet,
        ShippingCity = _shippingCity,
        ShippingState = _shippingState,
        ShippingPostalCode = _shippingPostalCode,
        ShippingCountry = _shippingCountry,
        CreatedAt = _createdAt,
        ConfirmedAt = _confirmedAt,
        ShippedAt = _shippedAt,
        DeliveredAt = _deliveredAt,
        CancelledAt = _cancelledAt,
        Lines = _lines
    };
}
