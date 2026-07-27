using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;

namespace SoftwareLearningGuide.Core.Business.ValueObjects;

/// <summary>
/// Value Object que representa una dirección de envío.
/// Garantiza que todos los campos requeridos estén presentes y sean válidos.
/// Inmutable por diseño (record).
/// </summary>
public record Address {
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public Address(string street, string city, string state, string postalCode, string country) {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException(DomainErrors.AddressErrors.StreetCannotBeEmpty());

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException(DomainErrors.AddressErrors.CityCannotBeEmpty());

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException(DomainErrors.AddressErrors.StateCannotBeEmpty());

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException(DomainErrors.AddressErrors.PostalCodeCannotBeEmpty());

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException(DomainErrors.AddressErrors.CountryCannotBeEmpty());

        Street = street.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
    }

    /// <summary>
    /// Crea una instancia de Address con validación.
    /// </summary>
    public static Result<Address> Create(string street, string city, string state, string postalCode, string country) {
        try {
            return Result<Address>.Success(new Address(street, city, state, postalCode, country));
        }
        catch (ArgumentException ex) {
            return Result<Address>.Failure(ex.Message);
        }
    }

    public override string ToString() =>
        $"{Street}, {City}, {State} {PostalCode}, {Country}";
}
