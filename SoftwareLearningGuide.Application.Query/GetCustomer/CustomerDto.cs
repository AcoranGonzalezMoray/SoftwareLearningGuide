namespace SoftwareLearningGuide.Application.Query.GetCustomer;

/// <summary>
/// DTO de lectura para Customer.
/// </summary>
public sealed record CustomerDto {
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? DefaultStreet { get; init; }
    public string? DefaultCity { get; init; }
    public string? DefaultState { get; init; }
    public string? DefaultPostalCode { get; init; }
    public string? DefaultCountry { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int OrderCount { get; init; }
}
