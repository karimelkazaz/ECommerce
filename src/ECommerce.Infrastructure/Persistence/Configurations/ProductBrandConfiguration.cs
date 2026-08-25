using ECommerce.Domain.Entities;
using ECommerce.Domain.Entities.ProductsBrand;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations
{
    public sealed class ProductBrandConfiguration
    : IEntityTypeConfiguration<ProductBrand>
    {
        public void Configure(
            EntityTypeBuilder<ProductBrand> builder)
        {
            BaseEntityConfiguration.Configure<ProductBrand, ProductBrandId>(builder);

            builder.ToTable("Brands");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new ProductBrandId(value));

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(x => x.Name)
                .IsUnique();
        }
    }
}
