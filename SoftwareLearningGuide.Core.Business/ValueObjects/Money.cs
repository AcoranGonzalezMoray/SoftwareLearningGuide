using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Core.Business.ValueObjects;

/// <summary>
/// Value Object que representa una cantidad de dinero con moneda.
/// Garantiza que nunca se realicen operaciones con monedas incompatibles.
/// Inmutable por diseño (record).
/// </summary>
public record Money {
    private const decimal MinAmount = 0;

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency) {
        if (amount < MinAmount)
            throw new ArgumentException(DomainErrors.MoneyErrors.AmountCannotBeNegative(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException(DomainErrors.MoneyErrors.CurrencyCannotBeEmpty());

        if (currency.Length != 3)
            throw new ArgumentException(DomainErrors.MoneyErrors.CurrencyMustBeThreeCharacters());

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Crea una instancia de Money con validación.
    /// </summary>
    public static Result<Money> Create(decimal amount, string currency) {
        try {
            return Result<Money>.Success(new Money(amount, currency));
        }
        catch (ArgumentException ex) {
            return Result<Money>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Crea una instancia de Money con valor cero en USD.
    /// </summary>
    public static Money Zero(string currency = "USD") => new(0, currency);

    /// <summary>
    /// Crea una instancia de Money con valor cero en la moneda especificada.
    /// </summary>
    public static Result<Money> ZeroIn(string currency) {
        try {
            return Result<Money>.Success(new(0, currency));
        }
        catch (ArgumentException ex) {
            return Result<Money>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Suma dos monedas. Devuelve Failure si las monedas son diferentes.
    /// </summary>
    public Result<Money> Add(Money other) {
        if (other == null)
            return Result<Money>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

        if (Currency != other.Currency)
            return Result<Money>.Failure(
                DomainErrors.MoneyErrors.CannotAddDifferentCurrencies(Currency, other.Currency));

        try {
            return Result<Money>.Success(new Money(Amount + other.Amount, Currency));
        }
        catch (ArgumentException ex) {
            return Result<Money>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Resta dos monedas. Devuelve Failure si las monedas son diferentes.
    /// </summary>
    public Result<Money> Subtract(Money other) {
        if (other == null)
            return Result<Money>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

        if (Currency != other.Currency)
            return Result<Money>.Failure(
                DomainErrors.MoneyErrors.CannotSubtractDifferentCurrencies(Currency, other.Currency));

        var result = Amount - other.Amount;
        if (result < MinAmount)
            return Result<Money>.Failure(
                DomainErrors.MoneyErrors.SubtractionResultCannotBeNegative(result));

        try {
            return Result<Money>.Success(new Money(result, Currency));
        }
        catch (ArgumentException ex) {
            return Result<Money>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Multiplica la cantidad de dinero por un factor.
    /// </summary>
    public Result<Money> Multiply(decimal factor) {
        if (factor < 0)
            return Result<Money>.Failure(DomainErrors.MoneyErrors.MultiplyFactorCannotBeNegative(factor));

        try {
            return Result<Money>.Success(new Money(Amount * factor, Currency));
        }
        catch (ArgumentException ex) {
            return Result<Money>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Divide la cantidad de dinero por un divisor.
    /// </summary>
    public Result<Money> Divide(decimal divisor) {
        if (divisor == 0)
            return Result<Money>.Failure(DomainErrors.MoneyErrors.CannotDivideByZero());

        if (divisor < 0)
            return Result<Money>.Failure(DomainErrors.MoneyErrors.DivisorCannotBeNegative(divisor));

        try {
            return Result<Money>.Success(new Money(Amount / divisor, Currency));
        }
        catch (ArgumentException ex) {
            return Result<Money>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Compara si dos monedas son iguales (mismo monto y moneda).
    /// </summary>
    public bool IsEqual(Money other) {
        if (other == null)
            return false;

        return Amount == other.Amount && Currency == other.Currency;
    }

    /// <summary>
    /// Compara si este monto es mayor que otro. Devuelve Failure si las monedas son diferentes.
    /// </summary>
    public Result<bool> IsGreaterThan(Money other) {
        if (other == null)
            return Result<bool>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

        if (Currency != other.Currency)
            return Result<bool>.Failure(
                DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies(Currency, other.Currency));

        return Result<bool>.Success(Amount > other.Amount);
    }

    /// <summary>
    /// Compara si este monto es menor que otro. Devuelve Failure si las monedas son diferentes.
    /// </summary>
    public Result<bool> IsLessThan(Money other) {
        if (other == null)
            return Result<bool>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

        if (Currency != other.Currency)
            return Result<bool>.Failure(
                DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies(Currency, other.Currency));

        return Result<bool>.Success(Amount < other.Amount);
    }

    /// <summary>
    /// Compara si este monto es mayor o igual a otro.
    /// </summary>
    public Result<bool> IsGreaterThanOrEqual(Money other) {
        if (other == null)
            return Result<bool>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

        if (Currency != other.Currency)
            return Result<bool>.Failure(
                DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies(Currency, other.Currency));

        return Result<bool>.Success(Amount >= other.Amount);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
