using SoftwareLearningGuide.Application.Query.GetAllCustomer;

namespace SoftwareLearningGuide.Helper.Test.Builders;

public class CustomerSummaryDtoBuilder {
    private Guid _id = Guid.NewGuid();
    private string _firstName = "John";
    private string _lastName = "Doe";
    private string _email = "john@example.com";
    private DateTime _createdAt = new(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private int _orderCount;

    public CustomerSummaryDtoBuilder WithId(Guid id) { _id = id; return this; }
    public CustomerSummaryDtoBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public CustomerSummaryDtoBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public CustomerSummaryDtoBuilder WithEmail(string email) { _email = email; return this; }
    public CustomerSummaryDtoBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }
    public CustomerSummaryDtoBuilder WithOrderCount(int orderCount) { _orderCount = orderCount; return this; }

    public CustomerSummaryDto Build() => new() {
        Id = _id,
        FirstName = _firstName,
        LastName = _lastName,
        Email = _email,
        CreatedAt = _createdAt,
        OrderCount = _orderCount
    };
}
