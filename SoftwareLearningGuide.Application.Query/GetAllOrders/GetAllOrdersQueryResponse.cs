namespace SoftwareLearningGuide.Application.Query.GetAllOrders;

public sealed record GetAllOrdersQueryResponse {
    public List<OrderSummaryDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
}
