using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SoftwareLearningGuide.Core.Business.Aggregates;
using SoftwareLearningGuide.Core.Business.ValueObjects;
using SoftwareLearningGuide.Helper.Test.Builders.Domain;
using SoftwareLearningGuide.Helper.Tests.Helper;
using SoftwareLearningGuide.Infraestructure.Data.Context;
using SoftwareLearningGuide.Infraestructure.Data.Repositories;

namespace SoftwareLearningGuide.Infraestructure.Test;

public class OrderWriteRepositoryShould : EFDatabase<ApplicationDbContext> {
    [Test]
    public void Constructor_NullContext_ThrowsArgumentNullException() {
        var act = () => new OrderWriteRepository(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task AddAsync_PersistsOrder() {
        var repository = new OrderWriteRepository(Context);
        var customerId = CustomerId.Create();
        var productId = ProductId.Create();

        Context.Customers.Add(new CustomerBuilder().WithId(customerId).Build());
        Context.Products.Add(new ProductBuilder().WithId(productId).WithDescription("Desc").Build());
        await Context.SaveChangesAsync();

        var orderId = OrderId.Create();
        var address = new AddressBuilder().Build();
        var order = new Order(orderId, customerId, address);

        var product = await Context.Products.FirstAsync(p => p.Id == productId);
        order.AddProduct(product, 2);

        await repository.AddAsync(order, CancellationToken.None);
        await Context.SaveChangesAsync();

        var saved = await Context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        saved.Should().NotBeNull();
        saved!.CustomerId.Value.Should().Be(customerId.Value);
    }

    [Test]
    public async Task AddAsync_OrderWithoutLines_StillPersists() {
        var repository = new OrderWriteRepository(Context);
        var customerId = CustomerId.Create();

        Context.Customers.Add(new CustomerBuilder().WithId(customerId).Build());
        await Context.SaveChangesAsync();

        var orderId = OrderId.Create();
        var address = new AddressBuilder().Build();
        var order = new Order(orderId, customerId, address);

        await repository.AddAsync(order, CancellationToken.None);
        await Context.SaveChangesAsync();

        var saved = await Context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        saved.Should().NotBeNull();
        saved!.GetLineCount().Should().Be(0);
    }

    [Test]
    public async Task GetByIdAsync_ExistingOrder_ReturnsOrder() {
        var repository = new OrderWriteRepository(Context);
        var customerId = CustomerId.Create();
        var productId = ProductId.Create();

        Context.Customers.Add(new CustomerBuilder().WithId(customerId).Build());
        Context.Products.Add(new ProductBuilder().WithId(productId).WithDescription("Desc").Build());
        await Context.SaveChangesAsync();

        var orderId = OrderId.Create();
        var address = new AddressBuilder().Build();
        var order = new Order(orderId, customerId, address);

        var product = await Context.Products.FirstAsync(p => p.Id == productId);
        order.AddProduct(product, 2);

        await repository.AddAsync(order, CancellationToken.None);
        await Context.SaveChangesAsync();

        var found = await repository.GetByIdAsync(orderId.Value, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be(orderId);
        found.CustomerId.Value.Should().Be(customerId.Value);
    }

    [Test]
    public async Task GetByIdAsync_NonExistingOrder_ReturnsNull() {
        var repository = new OrderWriteRepository(Context);

        var found = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        found.Should().BeNull();
    }
}
