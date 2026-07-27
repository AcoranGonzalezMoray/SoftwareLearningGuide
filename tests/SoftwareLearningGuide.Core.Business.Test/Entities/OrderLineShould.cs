using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.Entities;

public class OrderLineShould {
    private static OrderLineId ValidId() => OrderLineId.Create();
    private static ProductId ValidProductId() => ProductId.Create();
    private static Money ValidPrice() => new(25m, "USD");

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
        var result = OrderLine.Create(ValidId(), productId, "Laptop", new Money(0m, "USD"), 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.UnitPriceMustBeGreaterThanZero(productId.Value));
    }

    [Test]
    public void Create_NullId_ThrowsArgumentNullException() {
        var act = () => new OrderLine(null!, ValidProductId(), "Laptop", ValidPrice(), 1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Create_NullProductId_ThrowsArgumentNullException() {
        var act = () => new OrderLine(ValidId(), null!, "Laptop", ValidPrice(), 1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Create_NullUnitPrice_ThrowsArgumentNullException() {
        var act = () => new OrderLine(ValidId(), ValidProductId(), "Laptop", null!, 1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void GetSubtotal_CalculatesCorrectly() {
        var line = new OrderLine(ValidId(), ValidProductId(), "Laptop", new Money(25m, "USD"), 3);

        var result = line.GetSubtotal();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(75m);
        result.Value!.Currency.Should().Be("USD");
    }

    [Test]
    public void GetSubtotal_WithSingleQuantity_ReturnsUnitPrice() {
        var line = new OrderLine(ValidId(), ValidProductId(), "Laptop", new Money(25m, "USD"), 1);

        var result = line.GetSubtotal();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(25m);
    }

    [Test]
    public void UpdateQuantity_ValidQuantity_ReturnsSuccess() {
        var line = new OrderLine(ValidId(), ValidProductId(), "Laptop", ValidPrice(), 2);

        var result = line.UpdateQuantity(5);

        result.IsSuccess.Should().BeTrue();
        line.Quantity.Should().Be(5);
    }

    [Test]
    public void UpdateQuantity_ZeroQuantity_ReturnsFailure() {
        var line = new OrderLine(ValidId(), ValidProductId(), "Laptop", ValidPrice(), 2);

        var result = line.UpdateQuantity(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(line.ProductId.Value));
        line.Quantity.Should().Be(2);
    }

    [Test]
    public void UpdateQuantity_NegativeQuantity_ReturnsFailure() {
        var line = new OrderLine(ValidId(), ValidProductId(), "Laptop", ValidPrice(), 2);

        var result = line.UpdateQuantity(-1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.OrderLine.QuantityMustBeGreaterThanZero(line.ProductId.Value));
        line.Quantity.Should().Be(2);
    }

    [Test]
    public void Equals_SameId_ReturnsTrue() {
        var id = ValidId();
        var productId = ValidProductId();
        var a = new OrderLine(id, productId, "Laptop", ValidPrice(), 2);
        var b = new OrderLine(id, productId, "Desktop", new Money(50m, "USD"), 5);

        a.Equals(b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentId_ReturnsFalse() {
        var productId = ValidProductId();
        var a = new OrderLine(ValidId(), productId, "Laptop", ValidPrice(), 2);
        var b = new OrderLine(ValidId(), productId, "Laptop", ValidPrice(), 2);

        a.Equals(b).Should().BeFalse();
    }

    [Test]
    public void Equals_NullObject_ReturnsFalse() {
        var line = new OrderLine(ValidId(), ValidProductId(), "Laptop", ValidPrice(), 2);

        line.Equals(null).Should().BeFalse();
    }
}
