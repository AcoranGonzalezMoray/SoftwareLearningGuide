namespace SoftwareLearningGuide.Core.Business.ValueObjects;

using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;

/// <summary>
/// Value Object que representa un ID de Cliente.
/// </summary>
public record CustomerId {
    public Guid Value { get; }

    private CustomerId(Guid value) {
        if (value == Guid.Empty)
            throw new ArgumentException(DomainErrors.IdErrors.CustomerIdCannotBeEmpty());

        Value = value;
    }

    public static CustomerId Create() => new(Guid.NewGuid());

    public static Result<CustomerId> From(Guid value) {
        try {
            return Result<CustomerId>.Success(new CustomerId(value));
        }
        catch (ArgumentException ex) {
            return Result<CustomerId>.Failure(ex.Message);
        }
    }

    public override string ToString() => Value.ToString();
}
