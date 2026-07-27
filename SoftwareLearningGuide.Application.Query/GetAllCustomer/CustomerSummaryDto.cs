namespace SoftwareLearningGuide.Application.Query.GetAllCustomer;

/// <summary>
/// DTO resumido para Customer en listados.
/// </summary>
public sealed record CustomerSummaryDto {
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int OrderCount { get; init; }
}
