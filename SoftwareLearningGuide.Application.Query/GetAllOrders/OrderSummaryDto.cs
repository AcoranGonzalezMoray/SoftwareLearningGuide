namespace SoftwareLearningGuide.Application.Query.GetAllOrders;

/// <summary>
/// DTO de lectura resumido para Order (sin lineas).
/// </summary>
public sealed record OrderSummaryDto {
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int LineCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
