using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Helper.Test.Builders.Domain;

public class CustomerBuilder {
    private CustomerId _id = CustomerId.Create();
    private string _firstName = "John";
    private string _lastName = "Doe";
    private Email _email = null!;
    private Address? _defaultShippingAddress;
    private bool _hasDefaultShippingAddress;

    public CustomerBuilder() {
        var emailResult = Email.Create("john@example.com");
        _email = emailResult.Value!;
    }

    public CustomerBuilder WithId(CustomerId id) { _id = id; return this; }
    public CustomerBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public CustomerBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public CustomerBuilder WithEmail(Email email) { _email = email; return this; }
    public CustomerBuilder WithEmail(string email) {
        var result = Email.Create(email);
        _email = result.Value!;
        return this;
    }
    public CustomerBuilder WithDefaultShippingAddress(Address address) {
        _defaultShippingAddress = address;
        _hasDefaultShippingAddress = true;
        return this;
    }
    public CustomerBuilder WithoutDefaultShippingAddress() {
        _defaultShippingAddress = null;
        _hasDefaultShippingAddress = false;
        return this;
    }

    public Result<Customer> BuildResult() {
        var result = Customer.Create(_id, _firstName, _lastName, _email);
        if (!result.IsSuccess)
            return result;

        if (_hasDefaultShippingAddress && _defaultShippingAddress is not null) {
            var addrResult = result.Value!.SetDefaultShippingAddress(_defaultShippingAddress);
            if (!addrResult.IsSuccess)
                return Result<Customer>.Failure(addrResult.Error!);
        }

        return result;
    }

    public Customer Build() {
        var result = BuildResult();
        if (!result.IsSuccess)
            throw new InvalidOperationException($"CustomerBuilder failed: {result.Error}");
        return result.Value!;
    }
}
