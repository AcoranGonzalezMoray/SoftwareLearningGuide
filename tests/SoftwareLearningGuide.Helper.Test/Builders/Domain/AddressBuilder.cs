using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Helper.Test.Builders.Domain;

public class AddressBuilder {
    private string _street = "123 Main St";
    private string _city = "Springfield";
    private string _state = "IL";
    private string _postalCode = "62704";
    private string _country = "US";

    public AddressBuilder WithStreet(string street) { _street = street; return this; }
    public AddressBuilder WithCity(string city) { _city = city; return this; }
    public AddressBuilder WithState(string state) { _state = state; return this; }
    public AddressBuilder WithPostalCode(string postalCode) { _postalCode = postalCode; return this; }
    public AddressBuilder WithCountry(string country) { _country = country; return this; }

    public Address Build() => Address.Create(_street, _city, _state, _postalCode, _country).Value;

    public Result<Address> BuildResult() => Address.Create(_street, _city, _state, _postalCode, _country);
}
