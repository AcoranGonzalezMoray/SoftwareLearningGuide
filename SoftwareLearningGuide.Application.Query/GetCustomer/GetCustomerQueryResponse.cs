namespace SoftwareLearningGuide.Application.Query.GetCustomer;

public sealed record GetCustomerQueryResponse {
    public CustomerDto Customer { get; set; } = default!;
}
