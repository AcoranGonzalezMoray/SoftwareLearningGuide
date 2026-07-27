namespace SoftwareLearningGuide.Core.Business.Entities;

using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

/// <summary>
/// Entidad que representa un Cliente en el dominio de e-commerce.
/// Tiene identidad única y agrupa información personal del cliente.
/// </summary>
public class Customer : ProduceEvents {
    public CustomerId Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public Address? DefaultShippingAddress { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    private Customer() { }

    public Customer(CustomerId id, string firstName, string lastName, Email email) {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException(DomainErrors.Customer.FirstNameCannotBeEmpty(id.Value));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException(DomainErrors.Customer.LastNameCannotBeEmpty(id.Value));

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        Id = id;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new CustomerCreatedDomainEvent {
            CustomerId = id.Value,
            Email = email.Value,
            Name = $"{firstName.Trim()} {lastName.Trim()}"
        });
    }

    /// <summary>
    /// Crea una instancia de Customer con validación.
    /// </summary>
    public static Result<Customer> Create(CustomerId id, string firstName, string lastName, Email email) {
        try {
            return Result<Customer>.Success(new Customer(id, firstName, lastName, email));
        }
        catch (Exception ex) {
            return Result<Customer>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Obtiene el nombre completo del cliente.
    /// </summary>
    public string GetFullName() => $"{FirstName} {LastName}";

    /// <summary>
    /// Actualiza el nombre del cliente.
    /// </summary>
    public Result UpdateFirstName(string newFirstName) {
        if (string.IsNullOrWhiteSpace(newFirstName))
            return Result.Failure(DomainErrors.Customer.FirstNameCannotBeEmpty(Id.Value));

        FirstName = newFirstName.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Actualiza el apellido del cliente.
    /// </summary>
    public Result UpdateLastName(string newLastName) {
        if (string.IsNullOrWhiteSpace(newLastName))
            return Result.Failure(DomainErrors.Customer.LastNameCannotBeEmpty(Id.Value));

        LastName = newLastName.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Actualiza el correo del cliente.
    /// </summary>
    public Result UpdateEmail(Email newEmail) {
        if (newEmail == null)
            return Result.Failure(DomainErrors.Customer.EmailCannotBeNull());

        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Establece la dirección predeterminada de envío para el cliente.
    /// </summary>
    public Result SetDefaultShippingAddress(Address address) {
        if (address == null)
            return Result.Failure(DomainErrors.Customer.ShippingAddressCannotBeNull());

        DefaultShippingAddress = address;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Limpia la dirección predeterminada de envío.
    /// </summary>
    public Result ClearDefaultShippingAddress() {
        DefaultShippingAddress = null;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Verifica si el cliente tiene una dirección de envío preestablecida.
    /// </summary>
    public bool HasDefaultShippingAddress() => DefaultShippingAddress != null;

    /// <summary>
    /// Verifica si el cliente tiene un perfil completo (nombre, apellido y email).
    /// </summary>
    public bool HasCompleteProfile() {
        return !string.IsNullOrWhiteSpace(FirstName)
            && !string.IsNullOrWhiteSpace(LastName)
            && Email != null;
    }

    public override bool Equals(object? obj) {
        if (obj is not Customer other)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
