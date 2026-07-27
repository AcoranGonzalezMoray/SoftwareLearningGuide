namespace SoftwareLearningGuide.Application.Query.GetProduct;

/// <summary>
/// DTO de lectura para Product. Proyecta los datos necesarios para la capa de presentacion.
/// </summary>
public sealed record ProductDto {
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
