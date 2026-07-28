using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.Entities;

public class ProductShould {
    private static ProductId ValidId() => ProductId.Create();
    private static Money ValidPrice() => Money.Create(99.99m, "USD").Value;

    [Test]
    public void Create_ValidProduct_ReturnsSuccess() {
        var id = ValidId();

        var result = Product.Create(id, "Laptop", "Gaming laptop", ValidPrice(), 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Laptop");
        result.Value!.Description.Should().Be("Gaming laptop");
        result.Value!.Price.Amount.Should().Be(99.99m);
        result.Value!.StockQuantity.Should().Be(10);
        result.Value!.Id.Should().Be(id);
    }

    [Test]
    public void Create_EmptyName_ReturnsFailure() {
        var id = ValidId();
        var result = Product.Create(id, "", "Description", ValidPrice(), 10);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.NameCannotBeEmpty(id.Value));
    }

    [Test]
    public void Create_WhitespaceName_ReturnsFailure() {
        var id = ValidId();
        var result = Product.Create(id, "   ", "Description", ValidPrice(), 10);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_EmptyDescription_ReturnsFailure() {
        var id = ValidId();
        var result = Product.Create(id, "Name", "", ValidPrice(), 10);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.DescriptionCannotBeEmpty(id.Value));
    }

    [Test]
    public void Create_ZeroPrice_ReturnsFailure() {
        var id = ValidId();
        var result = Product.Create(id, "Name", "Desc", Money.Create(0m, "USD").Value, 10);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.PriceMustBeGreaterThanZero(id.Value));
    }

    [Test]
    public void Create_NegativeStock_ReturnsFailure() {
        var id = ValidId();
        var result = Product.Create(id, "Name", "Desc", ValidPrice(), -1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.StockCannotBeNegative(id.Value));
    }

    [Test]
    public void Create_NullPrice_ReturnsFailure() {
        var result = Product.Create(ValidId(), "Name", "Desc", null!, 10);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_FiresProductCreatedDomainEvent() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductCreatedDomainEvent>();
    }

    [Test]
    public void UpdateName_ValidName_ReturnsSuccess() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdateName("Desktop");

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Desktop");
        product.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void UpdateName_EmptyName_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdateName("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.NameCannotBeEmpty(product.Id.Value));
        product.Name.Should().Be("Laptop");
    }

    [Test]
    public void UpdateName_WhitespaceName_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdateName("   ");

        result.IsSuccess.Should().BeFalse();
        product.Name.Should().Be("Laptop");
    }

    [Test]
    public void UpdateDescription_ValidDescription_ReturnsSuccess() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdateDescription("New description");

        result.IsSuccess.Should().BeTrue();
        product.Description.Should().Be("New description");
        product.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void UpdateDescription_EmptyDescription_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdateDescription("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.DescriptionCannotBeEmpty(product.Id.Value));
    }

    [Test]
    public void UpdatePrice_ValidPrice_ReturnsSuccess() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdatePrice(Money.Create(199.99m, "USD").Value);

        result.IsSuccess.Should().BeTrue();
        product.Price.Amount.Should().Be(199.99m);
    }

    [Test]
    public void UpdatePrice_ZeroPrice_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdatePrice(Money.Create(0m, "USD").Value);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.PriceMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void UpdatePrice_NullPrice_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdatePrice(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.PriceCannotBeNull());
    }

    [Test]
    public void AddStock_ValidQuantity_IncreasesStock() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.AddStock(3);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(8);
    }

    [Test]
    public void AddStock_ZeroQuantity_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.AddStock(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityToAddMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void AddStock_NegativeQuantity_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.AddStock(-1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityToAddMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void RemoveStock_ValidQuantity_DecreasesStock() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        var result = product.RemoveStock(3);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(7);
    }

    [Test]
    public void RemoveStock_InsufficientStock_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 2).Value;

        var result = product.RemoveStock(5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.InsufficientStock(product.Id.Value, 2, 5));
        product.StockQuantity.Should().Be(2);
    }

    [Test]
    public void RemoveStock_ZeroQuantity_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        var result = product.RemoveStock(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityToRemoveMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void RemoveStock_NegativeQuantity_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        var result = product.RemoveStock(-5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityToRemoveMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void RemoveStock_StockStaysAbove10_DoesNotFireLowStockEvent() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 20).Value;

        product.RemoveStock(5);

        product.DomainEvents
            .Should().NotContain(e => e is ProductStockLowDomainEvent);
    }

    [Test]
    public void RemoveStock_StockDropsToExactly10_FiresProductStockLowEvent() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 20).Value;

        product.RemoveStock(10);

        product.DomainEvents
            .Should().Contain(e => e is ProductStockLowDomainEvent);
        product.StockQuantity.Should().Be(10);
    }

    [Test]
    public void RemoveStock_StockDropsBelow10_FiresProductStockLowEvent() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 12).Value;

        product.RemoveStock(5);

        product.DomainEvents
            .Should().Contain(e => e is ProductStockLowDomainEvent);
    }

    [Test]
    public void IsInStock_WithStock_ReturnsTrue() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        product.IsInStock().Should().BeTrue();
    }

    [Test]
    public void IsInStock_WithZeroStock_ReturnsFalse() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 0).Value;

        product.IsInStock().Should().BeFalse();
    }

    [Test]
    public void HasSufficientStock_Sufficient_ReturnsTrue() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        product.HasSufficientStock(5).Should().BeTrue();
    }

    [Test]
    public void HasSufficientStock_ExactAmount_ReturnsTrue() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        product.HasSufficientStock(10).Should().BeTrue();
    }

    [Test]
    public void HasSufficientStock_Insufficient_ReturnsFalse() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 3).Value;

        product.HasSufficientStock(5).Should().BeFalse();
    }

    [Test]
    public void CanBePurchased_ValidQuantity_ReturnsTrue() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        var result = product.CanBePurchased(3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void CanBePurchased_InsufficientStock_ReturnsFalse() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 2).Value;

        var result = product.CanBePurchased(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void CanBePurchased_ZeroQuantity_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        var result = product.CanBePurchased(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void CanBePurchased_NegativeQuantity_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        var result = product.CanBePurchased(-1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void CalculateSubtotal_ValidQuantity_ReturnsCorrectAmount() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", Money.Create(25m, "USD").Value, 10).Value;

        var result = product.CalculateSubtotal(3);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(75m);
        result.Value!.Currency.Should().Be("USD");
    }

    [Test]
    public void CalculateSubtotal_ZeroQuantity_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value   ;

        var result = product.CalculateSubtotal(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void CalculateSubtotal_NegativeQuantity_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 10).Value;

        var result = product.CalculateSubtotal(-1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Equals_SameId_ReturnsTrue() {
        var id = ValidId();
        var a = Product.Create(id, "Laptop", "Desc", ValidPrice(), 5).Value;
        var b = Product.Create(id, "Desktop", "Other", Money.Create(10m, "USD").Value, 1).Value;

        a.Equals(b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentId_ReturnsFalse() {
        var a = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;
        var b = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        a.Equals(b).Should().BeFalse();
    }

    [Test]
    public void Equals_NullObject_ReturnsFalse() {
        var a = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        a.Equals(null).Should().BeFalse();
    }

    [Test]
    public void Equals_DifferentType_ReturnsFalse() {
        var a = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        a.Equals("not a product").Should().BeFalse();
    }

    [Test]
    public void Create_NullId_ReturnsFailure() {
        var result = Product.Create(null!, "Laptop", "Desc", ValidPrice(), 10);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_NullName_ReturnsFailure() {
        var result = Product.Create(ValidId(), null!, "Desc", ValidPrice(), 10);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_NullDescription_ReturnsFailure() {
        var result = Product.Create(ValidId(), "Laptop", null!, ValidPrice(), 10);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_WhitespaceDescription_ReturnsFailure() {
        var id = ValidId();
        var result = Product.Create(id, "Laptop", "   ", ValidPrice(), 10);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.DescriptionCannotBeEmpty(id.Value));
    }

    [Test]
    public void Create_NegativePrice_ReturnsFailure() {
        var result = Product.Create(ValidId(), "Laptop", "Desc", Money.Create(-10m, "USD").Value!, 10);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void UpdateDescription_WhitespaceDescription_ReturnsFailure() {
        var product = Product.Create(ValidId(), "Laptop", "Desc", ValidPrice(), 5).Value;

        var result = product.UpdateDescription("   ");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.DescriptionCannotBeEmpty(product.Id.Value));
    }
}
