using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.ValueObjects;

public class AddressShould {
    private static Address ValidAddress() => Address.Create("123 Main St", "Springfield", "IL", "62704", "US").Value;

    [Test]
    public void Create_ValidAddress_ReturnsSuccess() {
        var result = Address.Create("123 Main St", "Springfield", "IL", "62704", "US");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Street.Should().Be("123 Main St");
        result.Value!.City.Should().Be("Springfield");
        result.Value!.State.Should().Be("IL");
        result.Value!.PostalCode.Should().Be("62704");
        result.Value!.Country.Should().Be("US");
    }

    [Test]
    public void Create_EmptyStreet_ReturnsFailure() {
        var result = Address.Create("", "Springfield", "IL", "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.StreetCannotBeEmpty());
    }

    [Test]
    public void Create_EmptyCity_ReturnsFailure() {
        var result = Address.Create("123 Main St", "", "IL", "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.CityCannotBeEmpty());
    }

    [Test]
    public void Create_EmptyState_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", "", "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.StateCannotBeEmpty());
    }

    [Test]
    public void Create_EmptyPostalCode_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", "IL", "", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.PostalCodeCannotBeEmpty());
    }

    [Test]
    public void Create_EmptyCountry_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", "IL", "62704", "");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.CountryCannotBeEmpty());
    }

    [Test]
    public void Create_WhitespaceStreet_ReturnsFailure() {
        var result = Address.Create("   ", "Springfield", "IL", "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.StreetCannotBeEmpty());
    }

    [Test]
    public void Create_WhitespaceCity_ReturnsFailure() {
        var result = Address.Create("123 Main St", "   ", "IL", "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.CityCannotBeEmpty());
    }

    [Test]
    public void Create_WhitespaceState_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", "   ", "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.StateCannotBeEmpty());
    }

    [Test]
    public void Create_WhitespacePostalCode_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", "IL", "   ", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.PostalCodeCannotBeEmpty());
    }

    [Test]
    public void Create_WhitespaceCountry_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", "IL", "62704", "   ");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.CountryCannotBeEmpty());
    }

    [Test]
    public void Create_NullStreet_ReturnsFailure() {
        var result = Address.Create(null!, "Springfield", "IL", "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.StreetCannotBeEmpty());
    }

    [Test]
    public void Create_NullCity_ReturnsFailure() {
        var result = Address.Create("123 Main St", null!, "IL", "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.CityCannotBeEmpty());
    }

    [Test]
    public void Create_NullState_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", null!, "62704", "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.StateCannotBeEmpty());
    }

    [Test]
    public void Create_NullPostalCode_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", "IL", null!, "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.PostalCodeCannotBeEmpty());
    }

    [Test]
    public void Create_NullCountry_ReturnsFailure() {
        var result = Address.Create("123 Main St", "Springfield", "IL", "62704", null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.AddressErrors.CountryCannotBeEmpty());
    }

    [Test]
    public void Create_AllFieldsEmpty_ReturnsFailure() {
        var result = Address.Create("", "", "", "", "");

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_TrimsWhitespace() {
        var result = Address.Create("  123 Main St  ", "  Springfield  ", "  IL  ", "  62704  ", "  US  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Street.Should().Be("123 Main St");
        result.Value!.City.Should().Be("Springfield");
    }

    [Test]
    public void ToString_FormatsCorrectly() {
        var address = ValidAddress();

        address.ToString().Should().Be("123 Main St, Springfield, IL 62704, US");
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual() {
        var a = ValidAddress();
        var b = ValidAddress();

        a.Should().Be(b);
    }

    [Test]
    public void RecordEquality_DifferentValues_AreNotEqual() {
        var a = ValidAddress();
        var b = Address.Create("456 Oak Ave", "Chicago", "IL", "60601", "US").Value;

        a.Should().NotBe(b);
    }
}
