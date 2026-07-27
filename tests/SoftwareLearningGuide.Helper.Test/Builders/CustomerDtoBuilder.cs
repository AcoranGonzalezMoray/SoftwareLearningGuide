using SoftwareLearningGuide.Application.Query.GetCustomer;

namespace SoftwareLearningGuide.Helper.Test.Builders;

public class CustomerDtoBuilder {
    private Guid _id = Guid.NewGuid();
    private string _firstName = "John";
    private string _lastName = "Doe";
    private string _email = "john@example.com";
    private string? _defaultStreet = "123 Main St";
    private string? _defaultCity = "Springfield";
    private string? _defaultState = "IL";
    private string? _defaultPostalCode = "62704";
    private string? _defaultCountry = "US";
    private DateTime _createdAt = new(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private DateTime? _updatedAt;
    private int _orderCount;

    public CustomerDtoBuilder WithId(Guid id) { _id = id; return this; }
    public CustomerDtoBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public CustomerDtoBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public CustomerDtoBuilder WithEmail(string email) { _email = email; return this; }
    public CustomerDtoBuilder WithDefaultAddress(string? street, string? city, string? state, string? postalCode, string? country) {
        _defaultStreet = street; _defaultCity = city; _defaultState = state; _defaultPostalCode = postalCode; _defaultCountry = country;
        return this;
    }
    public CustomerDtoBuilder WithoutDefaultAddress() {
        _defaultStreet = null; _defaultCity = null; _defaultState = null; _defaultPostalCode = null; _defaultCountry = null;
        return this;
    }
    public CustomerDtoBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }
    public CustomerDtoBuilder WithUpdatedAt(DateTime? updatedAt) { _updatedAt = updatedAt; return this; }
    public CustomerDtoBuilder WithOrderCount(int orderCount) { _orderCount = orderCount; return this; }

    public CustomerDto Build() => new() {
        Id = _id,
        FirstName = _firstName,
        LastName = _lastName,
        Email = _email,
        DefaultStreet = _defaultStreet,
        DefaultCity = _defaultCity,
        DefaultState = _defaultState,
        DefaultPostalCode = _defaultPostalCode,
        DefaultCountry = _defaultCountry,
        CreatedAt = _createdAt,
        UpdatedAt = _updatedAt,
        OrderCount = _orderCount
    };
}
