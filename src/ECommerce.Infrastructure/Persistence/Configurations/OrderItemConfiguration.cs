using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        BaseEntityConfiguration.Configure<OrderItem, OrderItemId>(builder);

        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(
                id => id.Value,
                value => new OrderItemId(value));

        builder.Property(i => i.ProductId)
            .HasConversion(
                id => id.Value,
                value => new ProductId(value));

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(Product.MaxNameLength);

        builder.Property(i => i.PictureUrl)
            .IsRequired()
            .HasMaxLength(Product.MaxPictureUrlLength);

        builder.OwnsOne(i => i.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2);

            priceBuilder.Property(m => m.Currency)
                .HasMaxLength(3)
                .HasConversion(c => c.Code, code => Currency.FromCode(code));
        });

        builder.HasIndex(i => i.ProductId);
    }
}
