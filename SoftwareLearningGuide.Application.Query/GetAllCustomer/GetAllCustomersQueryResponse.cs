namespace SoftwareLearningGuide.Application.Query.GetAllCustomer;

public sealed record GetAllCustomersQueryResponse {
    public List<CustomerSummaryDto> Customers { get; set; } = new();
    public int TotalCount { get; set; }
}
