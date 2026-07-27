using SoftwareLearningGuide.Application.Query.GetAllOrders;

namespace SoftwareLearningGuide.Helper.Test.Builders;

public class OrderSummaryDtoBuilder {
    private Guid _id = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private string _customerName = "John Doe";
    private string _status = "0";
    private int _lineCount;
    private DateTime _createdAt = new(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    public OrderSummaryDtoBuilder WithId(Guid id) { _id = id; return this; }
    public OrderSummaryDtoBuilder WithCustomerId(Guid customerId) { _customerId = customerId; return this; }
    public OrderSummaryDtoBuilder WithCustomerName(string customerName) { _customerName = customerName; return this; }
    public OrderSummaryDtoBuilder WithStatus(string status) { _status = status; return this; }
    public OrderSummaryDtoBuilder WithLineCount(int lineCount) { _lineCount = lineCount; return this; }
    public OrderSummaryDtoBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }

    public OrderSummaryDto Build() => new() {
        Id = _id,
        CustomerId = _customerId,
        CustomerName = _customerName,
        Status = _status,
        LineCount = _lineCount,
        CreatedAt = _createdAt
    };
}
