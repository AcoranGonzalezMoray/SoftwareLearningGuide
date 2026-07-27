using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Infraestructure.Data.Configurations;

/// <summary>
/// Configuración de EF para la entidad Customer.
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer> {
    public void Configure(EntityTypeBuilder<Customer> builder) {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();

        builder.OwnsOne(c => c.Email, emailBuilder => {
            emailBuilder.Property(e => e.Value).HasColumnName("Email").HasMaxLength(254);
        });

        builder.OwnsOne(c => c.DefaultShippingAddress, addressBuilder => {
            addressBuilder.Property(a => a.Street).HasColumnName("DefaultStreet").HasMaxLength(200);
            addressBuilder.Property(a => a.City).HasColumnName("DefaultCity").HasMaxLength(100);
            addressBuilder.Property(a => a.State).HasColumnName("DefaultState").HasMaxLength(100);
            addressBuilder.Property(a => a.PostalCode).HasColumnName("DefaultPostalCode").HasMaxLength(20);
            addressBuilder.Property(a => a.Country).HasColumnName("DefaultCountry").HasMaxLength(100);
        });

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired(false);
    }
}
