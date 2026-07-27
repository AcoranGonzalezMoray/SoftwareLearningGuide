using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.Entities;

public class CustomerShould {
    private static CustomerId ValidId() => CustomerId.Create();
    private static Email ValidEmail() => Email.Create("test@example.com").Value!;

    [Test]
    public void Create_ValidCustomer_ReturnsSuccess() {
        var id = ValidId();

        var result = Customer.Create(id, "John", "Doe", ValidEmail());

        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("John");
        result.Value!.LastName.Should().Be("Doe");
        result.Value!.Email.Value.Should().Be("test@example.com");
        result.Value!.Id.Should().Be(id);
    }

    [Test]
    public void Create_EmptyFirstName_ReturnsFailure() {
        var id = ValidId();
        var result = Customer.Create(id, "", "Doe", ValidEmail());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Customer.FirstNameCannotBeEmpty(id.Value));
    }

    [Test]
    public void Create_WhitespaceFirstName_ReturnsFailure() {
        var id = ValidId();
        var result = Customer.Create(id, "   ", "Doe", ValidEmail());

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_EmptyLastName_ReturnsFailure() {
        var id = ValidId();
        var result = Customer.Create(id, "John", "", ValidEmail());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Customer.LastNameCannotBeEmpty(id.Value));
    }

    [Test]
    public void Create_NullEmail_ThrowsArgumentNullException() {
        var act = () => new Customer(ValidId(), "John", "Doe", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Create_NullId_ThrowsArgumentNullException() {
        var act = () => new Customer(null!, "John", "Doe", ValidEmail());

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Create_FiresCustomerCreatedDomainEvent() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        customer.DomainEvents.Should().HaveCount(1);
        customer.DomainEvents.First().Should().BeOfType<CustomerCreatedDomainEvent>();
    }

    [Test]
    public void GetFullName_ReturnsConcatenatedName() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        customer.GetFullName().Should().Be("John Doe");
    }

    [Test]
    public void GetFullName_WithWhiteSpaceInNames_TrimsCorrectly() {
        var customer = new Customer(ValidId(), "  John  ", "  Doe  ", ValidEmail());

        customer.GetFullName().Should().Be("John Doe");
    }

    [Test]
    public void UpdateFirstName_ValidName_ReturnsSuccess() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        var result = customer.UpdateFirstName("Jane");

        result.IsSuccess.Should().BeTrue();
        customer.FirstName.Should().Be("Jane");
        customer.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void UpdateFirstName_EmptyName_ReturnsFailure() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        var result = customer.UpdateFirstName("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Customer.FirstNameCannotBeEmpty(customer.Id.Value));
        customer.FirstName.Should().Be("John");
    }

    [Test]
    public void UpdateFirstName_WhitespaceName_ReturnsFailure() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        var result = customer.UpdateFirstName("   ");

        result.IsSuccess.Should().BeFalse();
        customer.FirstName.Should().Be("John");
    }

    [Test]
    public void UpdateLastName_ValidName_ReturnsSuccess() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        var result = customer.UpdateLastName("Smith");

        result.IsSuccess.Should().BeTrue();
        customer.LastName.Should().Be("Smith");
        customer.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void UpdateLastName_EmptyName_ReturnsFailure() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        var result = customer.UpdateLastName("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Customer.LastNameCannotBeEmpty(customer.Id.Value));
        customer.LastName.Should().Be("Doe");
    }

    [Test]
    public void UpdateEmail_ValidEmail_ReturnsSuccess() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());
        var newEmail = Email.Create("new@example.com").Value!;

        var result = customer.UpdateEmail(newEmail);

        result.IsSuccess.Should().BeTrue();
        customer.Email.Value.Should().Be("new@example.com");
    }

    [Test]
    public void UpdateEmail_NullEmail_ReturnsFailure() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        var result = customer.UpdateEmail(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Customer.EmailCannotBeNull());
    }

    [Test]
    public void SetDefaultShippingAddress_ValidAddress_ReturnsSuccess() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());
        var address = new Address("123 Main St", "Springfield", "IL", "62704", "US");

        var result = customer.SetDefaultShippingAddress(address);

        result.IsSuccess.Should().BeTrue();
        customer.DefaultShippingAddress.Should().Be(address);
        customer.HasDefaultShippingAddress().Should().BeTrue();
        customer.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void SetDefaultShippingAddress_NullAddress_ReturnsFailure() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        var result = customer.SetDefaultShippingAddress(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Customer.ShippingAddressCannotBeNull());
    }

    [Test]
    public void ClearDefaultShippingAddress_WithNoAddress_ReturnsSuccess() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        var result = customer.ClearDefaultShippingAddress();

        result.IsSuccess.Should().BeTrue();
        customer.DefaultShippingAddress.Should().BeNull();
    }

    [Test]
    public void ClearDefaultShippingAddress_RemovesAddress() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());
        var address = new Address("123 Main St", "Springfield", "IL", "62704", "US");
        customer.SetDefaultShippingAddress(address);

        var result = customer.ClearDefaultShippingAddress();

        result.IsSuccess.Should().BeTrue();
        customer.DefaultShippingAddress.Should().BeNull();
        customer.HasDefaultShippingAddress().Should().BeFalse();
        customer.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void HasDefaultShippingAddress_WithAddress_ReturnsTrue() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());
        customer.SetDefaultShippingAddress(new Address("123 Main St", "Springfield", "IL", "62704", "US"));

        customer.HasDefaultShippingAddress().Should().BeTrue();
    }

    [Test]
    public void HasDefaultShippingAddress_WithoutAddress_ReturnsFalse() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        customer.HasDefaultShippingAddress().Should().BeFalse();
    }

    [Test]
    public void HasCompleteProfile_WithAllFields_ReturnsTrue() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        customer.HasCompleteProfile().Should().BeTrue();
    }

    [Test]
    public void Equals_SameId_ReturnsTrue() {
        var id = ValidId();
        var a = new Customer(id, "John", "Doe", ValidEmail());
        var b = new Customer(id, "Jane", "Smith", Email.Create("other@example.com").Value!);

        a.Equals(b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentId_ReturnsFalse() {
        var a = new Customer(ValidId(), "John", "Doe", ValidEmail());
        var b = new Customer(ValidId(), "John", "Doe", ValidEmail());

        a.Equals(b).Should().BeFalse();
    }

    [Test]
    public void Equals_NullObject_ReturnsFalse() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        customer.Equals(null).Should().BeFalse();
    }

    [Test]
    public void Equals_DifferentType_ReturnsFalse() {
        var customer = new Customer(ValidId(), "John", "Doe", ValidEmail());

        customer.Equals("not a customer").Should().BeFalse();
    }
}
