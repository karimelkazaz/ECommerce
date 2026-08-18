using ECommerce.Domain.Entities.ProductsType;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class ProductTypeConfiguration
: IEntityTypeConfiguration<ProductType>
{
    public void Configure(
        EntityTypeBuilder<ProductType> builder)
    {
        BaseEntityConfiguration.Configure<ProductType, ProductTypeId>(builder);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ProductTypeId(value));

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.Name)
            .IsUnique();
        
    }
}