using ApnaBazaar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApnaBazaar.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(
            "Products",
            table => table.HasCheckConstraint("CK_Products_Price", "[Price] >= 0")
        );

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SKU).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(60);
        builder.Property(p => p.PackSize).IsRequired().HasMaxLength(120);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");

        builder.HasIndex(p => p.SKU).IsUnique();
    }
}
