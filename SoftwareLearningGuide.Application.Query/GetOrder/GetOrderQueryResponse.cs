
using SoftwareLearningGuide.Application.Query.GetOrder;

namespace SoftwareLearningGuide.Application.Query.GetOrderQuery {
    public sealed record GetOrderQueryResponse {
        public OrderDto Order { get; set; }
    }
}
