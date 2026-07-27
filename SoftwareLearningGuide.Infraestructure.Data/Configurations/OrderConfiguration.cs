using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareLearningGuide.Core.Business.Aggregates;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Infraestructure.Data.Configurations;

/// <summary>
/// Configuración de EF para la entidad Order (Agregado).
/// Mapea ValueObjects como Owned Types y usa el campo privado _lines para la colección.
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order> {
    public void Configure(EntityTypeBuilder<Order> builder) {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerId)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value));

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.OwnsOne(o => o.ShippingAddress, addressBuilder => {
            addressBuilder.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200);
            addressBuilder.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100);
            addressBuilder.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(100);
            addressBuilder.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20);
            addressBuilder.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100);
        });

        builder.Ignore(o => o.Lines);

        builder.HasMany<OrderLine>("_lines")
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.ConfirmedAt).IsRequired(false);
        builder.Property(o => o.ShippedAt).IsRequired(false);
        builder.Property(o => o.DeliveredAt).IsRequired(false);
        builder.Property(o => o.CancelledAt).IsRequired(false);
    }
}
