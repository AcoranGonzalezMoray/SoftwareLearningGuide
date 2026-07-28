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
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(50m, "USD").Value;

        var result = a.Add(b);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(150m);
    }

    [Test]
    public void Add_WithZero_ReturnsSame() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Zero();

        var result = a.Add(b);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100m);
    }

    [Test]
    public void Add_DifferentCurrencies_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(50m, "EUR").Value;

        var result = a.Add(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotAddDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void Add_NullMoney_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;

        var result = a.Add(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void Subtract_SameCurrency_ReturnsDifference() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(30m, "USD").Value;

        var result = a.Subtract(b);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(70m);
    }

    [Test]
    public void Subtract_EqualAmount_ReturnsZero() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.Subtract(b);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(0m);
    }

    [Test]
    public void Subtract_ResultNegative_ReturnsFailure() {
        var a = Money.Create(30m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.Subtract(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("negativo");
    }

    [Test]
    public void Subtract_DifferentCurrencies_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(50m, "EUR").Value;

        var result = a.Subtract(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotSubtractDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void Subtract_NullMoney_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;

        var result = a.Subtract(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void Multiply_PositiveFactor_ReturnsProduct() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Multiply(3);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(300m);
    }

    [Test]
    public void Multiply_ByZero_ReturnsZero() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Multiply(0);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(0m);
    }

    [Test]
    public void Multiply_ByOne_ReturnsSame() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Multiply(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100m);
    }

    [Test]
    public void Multiply_ByDecimalFactor() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Multiply(0.5m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(50m);
    }

    [Test]
    public void Multiply_NegativeFactor_ReturnsFailure() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Multiply(-1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.MultiplyFactorCannotBeNegative(-1));
    }

    [Test]
    public void Divide_ByPositiveNumber_ReturnsQuotient() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Divide(4);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(25m);
    }

    [Test]
    public void Divide_ByOne_ReturnsSame() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Divide(1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100m);
    }

    [Test]
    public void Divide_ByDecimalFactor() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Divide(0.5m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(200m);
    }

    [Test]
    public void Divide_ByZero_ReturnsFailure() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Divide(0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotDivideByZero());
    }

    [Test]
    public void Divide_ByNegative_ReturnsFailure() {
        var money = Money.Create(100m, "USD").Value;

        var result = money.Divide(-2);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.DivisorCannotBeNegative(-2));
    }

    [Test]
    public void IsGreaterThan_SameCurrency_ReturnsTrue() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(50m, "USD").Value;

        var result = a.IsGreaterThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void IsGreaterThan_EqualAmount_ReturnsFalse() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.IsGreaterThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsGreaterThan_SmallerAmount_ReturnsFalse() {
        var a = Money.Create(50m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.IsGreaterThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsGreaterThan_DifferentCurrencies_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(50m, "EUR").Value;

        var result = a.IsGreaterThan(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void IsGreaterThan_NullMoney_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;

        var result = a.IsGreaterThan(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void IsLessThan_SameCurrency_ReturnsTrue() {
        var a = Money.Create(50m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.IsLessThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void IsLessThan_EqualAmount_ReturnsFalse() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.IsLessThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsLessThan_GreaterAmount_ReturnsFalse() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(50m, "USD").Value;

        var result = a.IsLessThan(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsLessThan_DifferentCurrencies_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(50m, "EUR").Value;

        var result = a.IsLessThan(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void IsLessThan_NullMoney_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;

        var result = a.IsLessThan(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void IsGreaterThanOrEqual_EqualAmount_ReturnsTrue() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.IsGreaterThanOrEqual(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void IsGreaterThanOrEqual_GreaterAmount_ReturnsTrue() {
        var a = Money.Create(200m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.IsGreaterThanOrEqual(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public void IsGreaterThanOrEqual_LesserAmount_ReturnsFalse() {
        var a = Money.Create(50m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        var result = a.IsGreaterThanOrEqual(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Test]
    public void IsGreaterThanOrEqual_NullMoney_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;

        var result = a.IsGreaterThanOrEqual(null!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());
    }

    [Test]
    public void IsGreaterThanOrEqual_DifferentCurrencies_ReturnsFailure() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(100m, "EUR").Value;

        var result = a.IsGreaterThanOrEqual(b);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies("USD", "EUR"));
    }

    [Test]
    public void IsEqual_SameAmountAndCurrency_ReturnsTrue() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        a.IsEqual(b).Should().BeTrue();
    }

    [Test]
    public void IsEqual_SameAmountDifferentCurrency_ReturnsFalse() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(100m, "EUR").Value;

        a.IsEqual(b).Should().BeFalse();
    }

    [Test]
    public void IsEqual_DifferentAmount_ReturnsFalse() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(200m, "USD").Value;

        a.IsEqual(b).Should().BeFalse();
    }

    [Test]
    public void IsEqual_NullMoney_ReturnsFalse() {
        var a = Money.Create(100m, "USD").Value;

        a.IsEqual(null!).Should().BeFalse();
    }

    [Test]
    public void ToString_FormatsCorrectly() {
        var money = Money.Create(123.456m, "USD").Value;

        money.ToString().Should().Match("*123*46 USD*");
    }

    [Test]
    public void ToString_ZeroAmount() {
        var money = Money.Zero();

        money.ToString().Should().Match("*0*00 USD*");
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(100m, "USD").Value;

        a.Should().Be(b);
    }

    [Test]
    public void RecordEquality_DifferentValues_AreNotEqual() {
        var a = Money.Create(100m, "USD").Value;
        var b = Money.Create(200m, "USD").Value;

        a.Should().NotBe(b);
    }
}
