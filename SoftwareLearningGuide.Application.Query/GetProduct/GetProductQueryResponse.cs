namespace SoftwareLearningGuide.Application.Query.GetProduct;

public sealed record GetProductQueryResponse {
    public ProductDto Product { get; set; } = default!;
}
