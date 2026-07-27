using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SoftwareLearningGuide.Helper.Test.Builders.Domain;
using SoftwareLearningGuide.Helper.Tests.Helper;
using SoftwareLearningGuide.Infraestructure.Data.Context;
using SoftwareLearningGuide.Infraestructure.Data.Repositories;

namespace SoftwareLearningGuide.Infraestructure.Test;

public class ProductWriteRepositoryShould : EFDatabase<ApplicationDbContext> {
    [Test]
    public void Constructor_NullContext_ThrowsArgumentNullException() {
        var act = () => new ProductWriteRepository(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task AddAsync_PersistsProduct() {
        var repository = new ProductWriteRepository(Context);
        var product = new ProductBuilder()
            .WithName("Gaming Mouse")
            .WithDescription("Wireless gaming mouse")
            .WithPrice(49.99m, "USD")
            .WithStockQuantity(25)
            .Build();

        await repository.AddAsync(product, CancellationToken.None);
        await Context.SaveChangesAsync();

        var saved = await Context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Gaming Mouse");
        saved.Price.Amount.Should().Be(49.99m);
    }

    [Test]
    public async Task GetByIdAsync_ExistingProduct_ReturnsProduct() {
        var repository = new ProductWriteRepository(Context);
        var product = new ProductBuilder()
            .WithName("Mechanical Keyboard")
            .WithDescription("RGB mechanical keyboard")
            .WithPrice(150m, "EUR")
            .WithStockQuantity(5)
            .Build();

        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var found = await repository.GetByIdAsync(product.Id.Value, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be(product.Id);
        found.Name.Should().Be("Mechanical Keyboard");
    }

    [Test]
    public async Task GetByIdAsync_NonExistingProduct_ReturnsNull() {
        var repository = new ProductWriteRepository(Context);

        var found = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        found.Should().BeNull();
    }
}
