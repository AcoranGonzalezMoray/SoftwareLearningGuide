using AwesomeAssertions;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Core.Business.Test.ValueObjects;

public class MoneyShould {
    [Test]
    public void Create_ValidAmountAndCurrency_ReturnsSuccess() {
        var result = Money.Create(100m, "USD");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100m);
        result.Value!.Currency.Should().Be("USD");
    }

    [Test]
    public void Create_NegativeAmount_ReturnsFailure() {
        var result = Money.Create(-10m, "USD");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.AmountCannotBeNegative(-10m));
    }

    [Test]
    public void Create_NegativeAmount_ErrorMessageIncludesAmount() {
        var result = Money.Create(-5m, "USD");

        result.Error.Should().Contain("-5");
    }

    [Test]
    public void Create_EmptyCurrency_ReturnsFailure() {
        var result = Money.Create(100m, "");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CurrencyCannotBeEmpty());
    }

    [Test]
    public void Create_CurrencyWithWrongLength_ReturnsFailure() {
        var result = Money.Create(100m, "US");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CurrencyMustBeThreeCharacters());
    }

    [Test]
    public void Create_NullCurrency_ReturnsFailure() {
        var result = Money.Create(100m, null!);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Create_LowercaseCurrency_ConvertsToUpper() {
        var result = Money.Create(100m, "eur");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Currency.Should().Be("EUR");
    }

    [Test]
    public void Zero_ReturnsZeroInUSD() {
        var money = Money.Zero();

        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("USD");
    }

    [Test]
    public void ZeroIn_ReturnsZeroInSpecifiedCurrency() {
        var result = Money.ZeroIn("EUR");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(0m);
        result.Value!.Currency.Should().Be("EUR");
    }

    [Test]
    public void ZeroIn_InvalidCurrency_ReturnsFailure() {
        var result = Money.ZeroIn("");

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void Add_SameCurrency_ReturnsSum() {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        var result = a.Add(b);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(150m);
    }

    [Test]
    public void Add_WithZero_ReturnsSame() {
        var a = new Money(100m, "USD");
        var b = Money.Zero();

        var result = a.Add(b);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100m);
    }

    [Test]
    public void Add_DifferentCurrencies_ReturnsFailure() {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "EUR");

        var result = a.Add(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotAddDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void Add_NullMoney_ReturnsFailure() {
        var a = new Money(100m, "USD");

        var result = a.Add(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void Subtract_SameCurrency_ReturnsDifference() {
        var a = new Money(100m, "USD");
        var b = new Money(30m, "USD");

        var result = a.Subtract(b);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(70m);
    }

    [Test]
    public void Subtract_EqualAmount_ReturnsZero() {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        var result = a.Subtract(b);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(0m);
    }

    [Test]
    public void Subtract_ResultNegative_ReturnsFailure() {
        var a = new Money(30m, "USD");
        var b = new Money(100m, "USD");

        var result = a.Subtract(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("negativo");
    }

    [Test]
    public void Subtract_DifferentCurrencies_ReturnsFailure() {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "EUR");

        var result = a.Subtract(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotSubtractDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void Subtract_NullMoney_ReturnsFailure() {
        var a = new Money(100m, "USD");

        var result = a.Subtract(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void Multiply_PositiveFactor_ReturnsProduct() {
        var money = new Money(100m, "USD");

        var result = money.Multiply(3);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(300m);
    }

    [Test]
    public void Multiply_ByZero_ReturnsZero() {
        var money = new Money(100m, "USD");

        var result = money.Multiply(0);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(0m);
    }

    [Test]
    public void Multiply_ByOne_ReturnsSame() {
        var money = new Money(100m, "USD");

        var result = money.Multiply(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100m);
    }

    [Test]
    public void Multiply_ByDecimalFactor() {
        var money = new Money(100m, "USD");

        var result = money.Multiply(0.5m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(50m);
    }

    [Test]
    public void Multiply_NegativeFactor_ReturnsFailure() {
        var money = new Money(100m, "USD");

        var result = money.Multiply(-1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.MultiplyFactorCannotBeNegative(-1));
    }

    [Test]
    public void Divide_ByPositiveNumber_ReturnsQuotient() {
        var money = new Money(100m, "USD");

        var result = money.Divide(4);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(25m);
    }

    [Test]
    public void Divide_ByOne_ReturnsSame() {
        var money = new Money(100m, "USD");

        var result = money.Divide(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100m);
    }

    [Test]
    public void Divide_ByDecimalFactor() {
        var money = new Money(100m, "USD");

        var result = money.Divide(0.5m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(200m);
    }

    [Test]
    public void Divide_ByZero_ReturnsFailure() {
        var money = new Money(100m, "USD");

        var result = money.Divide(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotDivideByZero());
    }

    [Test]
    public void Divide_ByNegative_ReturnsFailure() {
        var money = new Money(100m, "USD");

        var result = money.Divide(-2);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.DivisorCannotBeNegative(-2));
    }

    [Test]
    public void IsGreaterThan_SameCurrency_ReturnsTrue() {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        var result = a.IsGreaterThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void IsGreaterThan_EqualAmount_ReturnsFalse() {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        var result = a.IsGreaterThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsGreaterThan_SmallerAmount_ReturnsFalse() {
        var a = new Money(50m, "USD");
        var b = new Money(100m, "USD");

        var result = a.IsGreaterThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsGreaterThan_DifferentCurrencies_ReturnsFailure() {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "EUR");

        var result = a.IsGreaterThan(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void IsGreaterThan_NullMoney_ReturnsFailure() {
        var a = new Money(100m, "USD");

        var result = a.IsGreaterThan(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void IsLessThan_SameCurrency_ReturnsTrue() {
        var a = new Money(50m, "USD");
        var b = new Money(100m, "USD");

        var result = a.IsLessThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void IsLessThan_EqualAmount_ReturnsFalse() {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        var result = a.IsLessThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsLessThan_GreaterAmount_ReturnsFalse() {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        var result = a.IsLessThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsLessThan_DifferentCurrencies_ReturnsFailure() {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "EUR");

        var result = a.IsLessThan(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void IsLessThan_NullMoney_ReturnsFailure() {
        var a = new Money(100m, "USD");

        var result = a.IsLessThan(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void IsGreaterThanOrEqual_EqualAmount_ReturnsTrue() {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        var result = a.IsGreaterThanOrEqual(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void IsGreaterThanOrEqual_GreaterAmount_ReturnsTrue() {
        var a = new Money(200m, "USD");
        var b = new Money(100m, "USD");

        var result = a.IsGreaterThanOrEqual(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void IsGreaterThanOrEqual_LesserAmount_ReturnsFalse() {
        var a = new Money(50m, "USD");
        var b = new Money(100m, "USD");

        var result = a.IsGreaterThanOrEqual(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsGreaterThanOrEqual_NullMoney_ReturnsFailure() {
        var a = new Money(100m, "USD");

        var result = a.IsGreaterThanOrEqual(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void IsGreaterThanOrEqual_DifferentCurrencies_ReturnsFailure() {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "EUR");

        var result = a.IsGreaterThanOrEqual(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void IsEqual_SameAmountAndCurrency_ReturnsTrue() {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        a.IsEqual(b).Should().BeTrue();
    }

    [Test]
    public void IsEqual_SameAmountDifferentCurrency_ReturnsFalse() {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "EUR");

        a.IsEqual(b).Should().BeFalse();
    }

    [Test]
    public void IsEqual_DifferentAmount_ReturnsFalse() {
        var a = new Money(100m, "USD");
        var b = new Money(200m, "USD");

        a.IsEqual(b).Should().BeFalse();
    }

    [Test]
    public void IsEqual_NullMoney_ReturnsFalse() {
        var a = new Money(100m, "USD");

        a.IsEqual(null!).Should().BeFalse();
    }

    [Test]
    public void ToString_FormatsCorrectly() {
        var money = new Money(123.456m, "USD");

        money.ToString().Should().Match("*123*46 USD*");
    }

    [Test]
    public void ToString_ZeroAmount() {
        var money = Money.Zero();

        money.ToString().Should().Match("*0*00 USD*");
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual() {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        a.Should().Be(b);
    }

    [Test]
    public void RecordEquality_DifferentValues_AreNotEqual() {
        var a = new Money(100m, "USD");
        var b = new Money(200m, "USD");

        a.Should().NotBe(b);
    }

    [Test]
    public void Constructor_NegativeAmount_ThrowsArgumentException() {
        var act = () => new Money(-1m, "USD");

        act.Should().Throw<ArgumentException>()
            .WithMessage(DomainErrors.MoneyErrors.AmountCannotBeNegative(-1m));
    }

    [Test]
    public void Constructor_EmptyCurrency_ThrowsArgumentException() {
        var act = () => new Money(100m, "");

        act.Should().Throw<ArgumentException>()
            .WithMessage(DomainErrors.MoneyErrors.CurrencyCannotBeEmpty());
    }

    [Test]
    public void Constructor_WrongLengthCurrency_ThrowsArgumentException() {
        var act = () => new Money(100m, "US");

        act.Should().Throw<ArgumentException>()
            .WithMessage(DomainErrors.MoneyErrors.CurrencyMustBeThreeCharacters());
    }
}
