using Microsoft.EntityFrameworkCore;
using SoftwareLearningGuide.Core.Business.Aggregates;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Infraestructure.Data.Entities;

namespace SoftwareLearningGuide.Infraestructure.Data.Context;

/// <summary>
/// DbContext de Entity Framework para el lado de Command (escritura).
/// Gestiona las entidades del dominio y sus configuraciones.
/// La tabla DomainOutboxMessages se gestiona via OutboxMessageConfiguration.
/// </summary>
public class ApplicationDbContext : DbContext {
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
