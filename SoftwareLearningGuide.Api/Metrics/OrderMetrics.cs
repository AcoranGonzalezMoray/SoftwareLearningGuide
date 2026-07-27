using System.Diagnostics.Metrics;

namespace SoftwareLearningGuide.Api.Metrics;

public sealed class OrderMetrics {
    private readonly Counter<long> _ordersCreated;

    public OrderMetrics(IMeterFactory meterFactory) {
        var meter = meterFactory.Create("SoftwareLearningGuide.Orders");
        _ordersCreated = meter.CreateCounter<long>("orders.created", "orders", "Total number of orders created");
    }

    public void OrderCreated(Guid orderId, Guid customerId) {
        _ordersCreated.Add(1, new KeyValuePair<string, object?>("order.id", orderId.ToString()),
                              new KeyValuePair<string, object?>("customer.id", customerId.ToString()));
    }
}
