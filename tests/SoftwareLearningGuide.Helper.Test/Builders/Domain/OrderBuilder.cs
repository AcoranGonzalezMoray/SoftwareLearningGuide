using SoftwareLearningGuide.Core.Business.Aggregates;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Helper.Test.Builders.Domain;

public class OrderBuilder {
    private OrderId _id = OrderId.Create();
    private CustomerId _customerId = CustomerId.Create();
    private Address _shippingAddress = null!;

    public OrderBuilder() {
        var addrResult = Address.Create("123 Main St", "Springfield", "IL", "62704", "US");
        _shippingAddress = addrResult.Value!;
    }

    public OrderBuilder WithId(OrderId id) { _id = id; return this; }
    public OrderBuilder WithCustomerId(CustomerId customerId) { _customerId = customerId; return this; }
    public OrderBuilder WithShippingAddress(Address address) { _shippingAddress = address; return this; }
    public OrderBuilder WithShippingAddress(string street, string city, string state, string postalCode, string country) {
        var result = Address.Create(street, city, state, postalCode, country);
        _shippingAddress = result.Value!;
        return this;
    }

    public Result<Order> BuildResult() => Order.Create(_id, _customerId, _shippingAddress);

    public Order Build() {
        var result = BuildResult();
        if (!result.IsSuccess)
            throw new InvalidOperationException($"OrderBuilder failed: {result.Error}");
        return result.Value!;
    }
}
