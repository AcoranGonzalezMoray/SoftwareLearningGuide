using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.Entities;

public class OrderLineShould {
    private static OrderLineId ValidId() => OrderLineId.Create();
    private static ProductId ValidProductId() => ProductId.Create();
    private static Money ValidPrice() => Money.Create(25m, "USD").Value;

    [Test]
    public void Create_ValidOrderLine_ReturnsSuccess() {
        var id = ValidId();
        var productId = ValidProductId();

        var result = OrderLine.Create(id, productId, "Laptop", ValidPrice(), 2);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductId.Should().Be(productId);
        result.Value!.ProductName.Should().Be("Laptop");
        result.Value!.UnitPrice.Amount.Should().Be(25m);
        result.Value!.Quantity.Should().Be(2);
    }

    [Test]
    public void Create_EmptyProductName_ReturnsFailure() {
        var productId = ValidProductId();
        var result = OrderLine.Create(ValidId(), productId, "", ValidPrice(), 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.ProductNameCannotBeEmpty(productId.Value));
    }

    [Test]
    public void Create_WhitespaceProductName_ReturnsFailure() {
        var productId = ValidProductId();
        var result = OrderLine.Create(ValidId(), productId, "   ", ValidPrice(), 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_ZeroQuantity_ReturnsFailure() {
        var productId = ValidProductId();
        var result = OrderLine.Create(ValidId(), productId, "Laptop", ValidPrice(), 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(productId.Value));
    }

    [Test]
    public void Create_NegativeQuantity_ReturnsFailure() {
        var productId = ValidProductId();
        var result = OrderLine.Create(ValidId(), productId, "Laptop", ValidPrice(), -1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(productId.Value));
    }

    [Test]
    public void Create_ZeroPrice_ReturnsFailure() {
        var productId = ValidProductId();
        var result = OrderLine.Create(ValidId(), productId, "Laptop", Money.Create(0m, "USD").Value, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.UnitPriceMustBeGreaterThanZero(productId.Value));
    }

    [Test]
    public void GetSubtotal_CalculatesCorrectly() {
        var line = OrderLine.Create(ValidId(), ValidProductId(), "Laptop", Money.Create(25m, "USD").Value, 3).Value;

        var result = line.GetSubtotal();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(75m);
        result.Value!.Currency.Should().Be("USD");
    }

    [Test]
    public void GetSubtotal_WithSingleQuantity_ReturnsUnitPrice() {
        var line = OrderLine.Create(ValidId(), ValidProductId(), "Laptop", Money.Create(25m, "USD").Value, 1).Value;

        var result = line.GetSubtotal();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(25m);
    }

    [Test]
    public void UpdateQuantity_ValidQuantity_ReturnsSuccess() {
        var line = OrderLine.Create(ValidId(), ValidProductId(), "Laptop", ValidPrice(), 2).Value;

        var result = line.UpdateQuantity(5);

        result.IsSuccess.Should().BeTrue();
        line.Quantity.Should().Be(5);
    }

    [Test]
    public void UpdateQuantity_ZeroQuantity_ReturnsFailure() {
        var line = OrderLine.Create(ValidId(), ValidProductId(), "Laptop", ValidPrice(), 2).Value;

        var result = line.UpdateQuantity(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(line.ProductId.Value));
        line.Quantity.Should().Be(2);
    }

    [Test]
    public void UpdateQuantity_NegativeQuantity_ReturnsFailure() {
        var line = OrderLine.Create(ValidId(), ValidProductId(), "Laptop", ValidPrice(), 2).Value;

        var result = line.UpdateQuantity(-1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(line.ProductId.Value));
        line.Quantity.Should().Be(2);
    }

    [Test]
    public void Equals_SameId_ReturnsTrue() {
        var id = ValidId();
        var productId = ValidProductId();
        var a = OrderLine.Create(id, productId, "Laptop", ValidPrice(), 2).Value;
        var b = OrderLine.Create(id, productId, "Desktop", Money.Create(50m, "USD").Value, 5).Value;

        a.Equals(b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentId_ReturnsFalse() {
        var productId = ValidProductId();
        var a = OrderLine.Create(ValidId(), productId, "Laptop", ValidPrice(), 2).Value;
        var b = OrderLine.Create(ValidId(), productId, "Laptop", ValidPrice(), 2).Value;

        a.Equals(b).Should().BeFalse();
    }

    [Test]
    public void Equals_NullObject_ReturnsFalse() {
        var line = OrderLine.Create(ValidId(), ValidProductId(), "Laptop", ValidPrice(), 2).Value;

        line.Equals(null).Should().BeFalse();
    }

    [Test]
    public void Create_NullId_ReturnsFailure() {
        var result = OrderLine.Create(null!, ValidProductId(), "Laptop", ValidPrice(), 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_NullProductId_ReturnsFailure() {
        var result = OrderLine.Create(ValidId(), null!, "Laptop", ValidPrice(), 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_NullUnitPrice_ReturnsFailure() {
        var result = OrderLine.Create(ValidId(), ValidProductId(), "Laptop", null!, 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_NegativePrice_ReturnsFailure() {
        var result = OrderLine.Create(ValidId(), ValidProductId(), "Laptop", Money.Create(-10m, "USD").Value!, 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_NullProductName_ReturnsFailure() {
        var productId = ValidProductId();
        var result = OrderLine.Create(ValidId(), productId, null!, ValidPrice(), 1);

        result.IsSuccess.Should().BeFalse();
    }
}
