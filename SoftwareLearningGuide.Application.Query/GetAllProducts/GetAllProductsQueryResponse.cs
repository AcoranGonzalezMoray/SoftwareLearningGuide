using SoftwareLearningGuide.Application.Query.GetProduct;

namespace SoftwareLearningGuide.Application.Query.GetAllProducts;

public sealed record GetAllProductsQueryResponse {
    public List<ProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
}
