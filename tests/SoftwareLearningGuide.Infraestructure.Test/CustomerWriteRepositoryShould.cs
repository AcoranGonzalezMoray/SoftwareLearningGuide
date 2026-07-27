using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SoftwareLearningGuide.Helper.Test.Builders.Domain;
using SoftwareLearningGuide.Helper.Tests.Helper;
using SoftwareLearningGuide.Infraestructure.Data.Context;
using SoftwareLearningGuide.Infraestructure.Data.Repositories;

namespace SoftwareLearningGuide.Infraestructure.Test;

public class CustomerWriteRepositoryShould : EFDatabase<ApplicationDbContext> {
    [Test]
    public void Constructor_NullContext_ThrowsArgumentNullException() {
        var act = () => new CustomerWriteRepository(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task AddAsync_PersistsCustomer() {
        var repository = new CustomerWriteRepository(Context);
        var customer = new CustomerBuilder()
            .WithFirstName("Alice")
            .WithLastName("Johnson")
            .WithEmail("alice@test.com")
            .Build();

        await repository.AddAsync(customer, CancellationToken.None);
        await Context.SaveChangesAsync();

        var saved = await Context.Customers
            .FirstOrDefaultAsync(c => c.Id == customer.Id);

        saved.Should().NotBeNull();
        saved!.FirstName.Should().Be("Alice");
        saved.LastName.Should().Be("Johnson");
    }

    [Test]
    public async Task AddAsync_PersistsCustomerWithAddress() {
        var repository = new CustomerWriteRepository(Context);
        var address = new AddressBuilder().Build();
        var customer = new CustomerBuilder()
            .WithFirstName("Bob")
            .WithLastName("Smith")
            .WithEmail("bob@test.com")
            .WithDefaultShippingAddress(address)
            .Build();

        await repository.AddAsync(customer, CancellationToken.None);
        await Context.SaveChangesAsync();

        var saved = await Context.Customers
            .FirstOrDefaultAsync(c => c.Id == customer.Id);

        saved.Should().NotBeNull();
        saved!.DefaultShippingAddress.Should().NotBeNull();
        saved.DefaultShippingAddress!.Street.Should().Be("123 Main St");
        saved.DefaultShippingAddress.City.Should().Be("Springfield");
    }

    [Test]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsCustomer() {
        var repository = new CustomerWriteRepository(Context);
        var customer = new CustomerBuilder()
            .WithFirstName("Charlie")
            .WithEmail("charlie@test.com")
            .Build();

        Context.Customers.Add(customer);
        await Context.SaveChangesAsync();

        var found = await repository.GetByIdAsync(customer.Id.Value, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be(customer.Id);
        found.Email.Value.Should().Be("charlie@test.com");
    }

    [Test]
    public async Task GetByIdAsync_NonExistingCustomer_ReturnsNull() {
        var repository = new CustomerWriteRepository(Context);

        var found = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        found.Should().BeNull();
    }
}
