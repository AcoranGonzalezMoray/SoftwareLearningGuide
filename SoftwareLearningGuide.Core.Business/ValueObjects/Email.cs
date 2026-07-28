namespace SoftwareLearningGuide.Core.Business.ValueObjects;

using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using System.Text.RegularExpressions;

/// <summary>
/// Value Object que representa un correo electrónico.
/// Valida el formato del email según especificación RFC 5322 simplificada.
/// Inmutable por diseño (record).
/// </summary>
public record Email {
    private const int MaxLength = 254;

    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value) {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(DomainErrors.EmailErrors.CannotBeEmpty());

        var trimmedEmail = value.Trim().ToLowerInvariant();

        if (trimmedEmail.Length > MaxLength)
            throw new ArgumentException(DomainErrors.EmailErrors.CannotExceedMaxLength(MaxLength));

        if (!EmailRegex.IsMatch(trimmedEmail))
            throw new ArgumentException(DomainErrors.EmailErrors.InvalidFormat(value));

        Value = trimmedEmail;
    }

    /// <summary>
    /// Crea una instancia de Email con validación.
    /// </summary>
    public static Result<Email> Create(string value) {
        try {
            return Result<Email>.Success(new Email(value));
        }
        catch (ArgumentException ex) {
            return Result<Email>.Failure(ex.Message);
        }
    }

    public override string ToString() => Value;
}
