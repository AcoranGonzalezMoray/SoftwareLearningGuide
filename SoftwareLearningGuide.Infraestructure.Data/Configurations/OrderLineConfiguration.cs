using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareLearningGuide.Core.Business.Entities;
using SoftwareLearningGuide.Core.Business.ValueObjects;

namespace SoftwareLearningGuide.Infraestructure.Data.Configurations;

/// <summary>
/// Configuración de EF para la entidad OrderLine.
/// Mapea el ValueObject Money como Owned Type en columnas UnitPrice y Currency.
/// </summary>
public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine> {
    public void Configure(EntityTypeBuilder<OrderLine> builder) {
        builder.ToTable("OrderLines");

        builder.HasKey(ol => ol.Id);

        builder.Property(ol => ol.Id)
            .HasConversion(
                id => id.Value,
                value => new OrderLineId(value))
            .ValueGeneratedNever();

        builder.Property(ol => ol.ProductId)
            .HasConversion(
                id => id.Value,
                value => new ProductId(value));

        builder.Property(ol => ol.ProductName).HasMaxLength(200).IsRequired();

        builder.OwnsOne(ol => ol.UnitPrice, moneyBuilder => {
            moneyBuilder.Property(m => m.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Property(ol => ol.Quantity).IsRequired();

        builder.Property(ol => ol.CreatedAt).IsRequired();
    }
}
