using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.Entities;

public class ProductShould {
    private static ProductId ValidId() => ProductId.Create();
    private static Money ValidPrice() => new(99.99m, "USD");

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
        var result = Product.Create(id, "Name", "Desc", new Money(0m, "USD"), 10);

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
    public void Create_NullId_ThrowsArgumentNullException() {
        var act = () => new Product(null!, "Name", "Desc", ValidPrice(), 5);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Create_FiresProductCreatedDomainEvent() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductCreatedDomainEvent>();
    }

    [Test]
    public void UpdateName_ValidName_ReturnsSuccess() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.UpdateName("Desktop");

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Desktop");
        product.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void UpdateName_EmptyName_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.UpdateName("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.NameCannotBeEmpty(product.Id.Value));
        product.Name.Should().Be("Laptop");
    }

    [Test]
    public void UpdateName_WhitespaceName_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.UpdateName("   ");

        result.IsSuccess.Should().BeFalse();
        product.Name.Should().Be("Laptop");
    }

    [Test]
    public void UpdateDescription_ValidDescription_ReturnsSuccess() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.UpdateDescription("New description");

        result.IsSuccess.Should().BeTrue();
        product.Description.Should().Be("New description");
        product.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public void UpdateDescription_EmptyDescription_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.UpdateDescription("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.DescriptionCannotBeEmpty(product.Id.Value));
    }

    [Test]
    public void UpdatePrice_ValidPrice_ReturnsSuccess() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.UpdatePrice(new Money(199.99m, "USD"));

        result.IsSuccess.Should().BeTrue();
        product.Price.Amount.Should().Be(199.99m);
    }

    [Test]
    public void UpdatePrice_ZeroPrice_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.UpdatePrice(new Money(0m, "USD"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.PriceMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void UpdatePrice_NullPrice_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.UpdatePrice(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.PriceCannotBeNull());
    }

    [Test]
    public void AddStock_ValidQuantity_IncreasesStock() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.AddStock(3);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(8);
    }

    [Test]
    public void AddStock_ZeroQuantity_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.AddStock(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityToAddMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void AddStock_NegativeQuantity_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        var result = product.AddStock(-1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityToAddMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void RemoveStock_ValidQuantity_DecreasesStock() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        var result = product.RemoveStock(3);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(7);
    }

    [Test]
    public void RemoveStock_InsufficientStock_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 2);

        var result = product.RemoveStock(5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.InsufficientStock(product.Id.Value, 2, 5));
        product.StockQuantity.Should().Be(2);
    }

    [Test]
    public void RemoveStock_ZeroQuantity_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        var result = product.RemoveStock(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityToRemoveMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void RemoveStock_NegativeQuantity_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        var result = product.RemoveStock(-5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityToRemoveMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void RemoveStock_StockStaysAbove10_DoesNotFireLowStockEvent() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 20);

        product.RemoveStock(5);

        product.DomainEvents
            .Should().NotContain(e => e is ProductStockLowDomainEvent);
    }

    [Test]
    public void RemoveStock_StockDropsToExactly10_FiresProductStockLowEvent() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 20);

        product.RemoveStock(10);

        product.DomainEvents
            .Should().Contain(e => e is ProductStockLowDomainEvent);
        product.StockQuantity.Should().Be(10);
    }

    [Test]
    public void RemoveStock_StockDropsBelow10_FiresProductStockLowEvent() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 12);

        product.RemoveStock(5);

        product.DomainEvents
            .Should().Contain(e => e is ProductStockLowDomainEvent);
    }

    [Test]
    public void IsInStock_WithStock_ReturnsTrue() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        product.IsInStock().Should().BeTrue();
    }

    [Test]
    public void IsInStock_WithZeroStock_ReturnsFalse() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 0);

        product.IsInStock().Should().BeFalse();
    }

    [Test]
    public void HasSufficientStock_Sufficient_ReturnsTrue() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        product.HasSufficientStock(5).Should().BeTrue();
    }

    [Test]
    public void HasSufficientStock_ExactAmount_ReturnsTrue() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        product.HasSufficientStock(10).Should().BeTrue();
    }

    [Test]
    public void HasSufficientStock_Insufficient_ReturnsFalse() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 3);

        product.HasSufficientStock(5).Should().BeFalse();
    }

    [Test]
    public void CanBePurchased_ValidQuantity_ReturnsTrue() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        var result = product.CanBePurchased(3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void CanBePurchased_InsufficientStock_ReturnsFalse() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 2);

        var result = product.CanBePurchased(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void CanBePurchased_ZeroQuantity_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        var result = product.CanBePurchased(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void CanBePurchased_NegativeQuantity_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        var result = product.CanBePurchased(-1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void CalculateSubtotal_ValidQuantity_ReturnsCorrectAmount() {
        var product = new Product(ValidId(), "Laptop", "Desc", new Money(25m, "USD"), 10);

        var result = product.CalculateSubtotal(3);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(75m);
        result.Value!.Currency.Should().Be("USD");
    }

    [Test]
    public void CalculateSubtotal_ZeroQuantity_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        var result = product.CalculateSubtotal(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Product.QuantityMustBeGreaterThanZero(product.Id.Value));
    }

    [Test]
    public void CalculateSubtotal_NegativeQuantity_ReturnsFailure() {
        var product = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 10);

        var result = product.CalculateSubtotal(-1);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Equals_SameId_ReturnsTrue() {
        var id = ValidId();
        var a = new Product(id, "Laptop", "Desc", ValidPrice(), 5);
        var b = new Product(id, "Desktop", "Other", new Money(10m, "USD"), 1);

        a.Equals(b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentId_ReturnsFalse() {
        var a = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);
        var b = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        a.Equals(b).Should().BeFalse();
    }

    [Test]
    public void Equals_NullObject_ReturnsFalse() {
        var a = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        a.Equals(null).Should().BeFalse();
    }

    [Test]
    public void Equals_DifferentType_ReturnsFalse() {
        var a = new Product(ValidId(), "Laptop", "Desc", ValidPrice(), 5);

        a.Equals("not a product").Should().BeFalse();
    }
}
