using ECommerce.Domain.Entities;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        BaseEntityConfiguration.Configure<Product, ProductId>(builder);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ProductId(value));

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Product.MaxNameLength);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(Product.MaxDescriptionLength);

        builder.Property(x => x.PictureUrl)
            .IsRequired()
            .HasMaxLength(Product.MaxPictureUrlLength);

        builder.OwnsOne(x => x.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2);

            priceBuilder.Property(m => m.Currency)
                .HasMaxLength(3)
                .HasConversion(c => c.Code, code => Currency.FromCode(code));

            priceBuilder.HasIndex(m => m.Amount);
        });
        builder.HasOne(x => x.ProductBrand)
            .WithMany(pb => pb.Products)
            .HasForeignKey(x => x.ProductBrandId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ProductType)
            .WithMany(pt => pt.Products)
            .HasForeignKey(x => x.ProductTypeId)
            .OnDelete(DeleteBehavior.NoAction);


        builder.HasIndex(x => x.Name);
    }
}