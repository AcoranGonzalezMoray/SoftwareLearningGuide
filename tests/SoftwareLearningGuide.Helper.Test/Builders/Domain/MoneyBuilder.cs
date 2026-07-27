using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Helper.Test.Builders.Domain;

public class MoneyBuilder {
    private decimal _amount = 100m;
    private string _currency = "USD";

    public MoneyBuilder WithAmount(decimal amount) { _amount = amount; return this; }
    public MoneyBuilder WithCurrency(string currency) { _currency = currency; return this; }

    public Money Build() => new(_amount, _currency);

    public Result<Money> BuildResult() => Money.Create(_amount, _currency);
}
