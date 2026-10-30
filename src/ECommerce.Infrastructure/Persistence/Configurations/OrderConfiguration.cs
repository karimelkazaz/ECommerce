using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        BaseEntityConfiguration.Configure<Order, OrderId>(builder);

        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value));

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Status);

        builder.Property(o => o.PaymentIntentId)
            .HasMaxLength(200);

        // Stripe intents are only ever linked to a single order; still nullable until checkout assigns it.
        builder.HasIndex(o => o.PaymentIntentId)
            .IsUnique()
            .HasFilter("[PaymentIntentId] IS NOT NULL");

        builder.HasIndex(o => new { o.UserId, o.Status });

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(500);

        builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.RecipientFirstName)
                .HasColumnName("ShippingFirstName")
                .IsRequired()
                .HasMaxLength(100);

            addressBuilder.Property(a => a.RecipientLastName)
                .HasColumnName("ShippingLastName")
                .IsRequired()
                .HasMaxLength(100);

            addressBuilder.Property(a => a.PhoneNumber)
                .HasColumnName("ShippingPhone")
                .IsRequired()
                .HasMaxLength(20);

            addressBuilder.Property(a => a.Country)
                .HasColumnName("ShippingCountry")
                .IsRequired()
                .HasMaxLength(100);

            addressBuilder.Property(a => a.City)
                .HasColumnName("ShippingCity")
                .IsRequired()
                .HasMaxLength(100);

            addressBuilder.Property(a => a.Street)
                .HasColumnName("ShippingStreet")
                .IsRequired()
                .HasMaxLength(200);

            addressBuilder.Property(a => a.PostalCode)
                .HasColumnName("ShippingPostalCode")
                .IsRequired()
                .HasMaxLength(20);
        });

        builder.OwnsOne(o => o.DeliveryMethod, deliveryBuilder =>
        {
            deliveryBuilder.Property(d => d.Name)
                .HasColumnName("DeliveryMethodName")
                .IsRequired()
                .HasMaxLength(DeliveryMethod.MaxNameLength);

            deliveryBuilder.Property(d => d.DeliveryTime)
                .HasColumnName("DeliveryTime")
                .IsRequired()
                .HasMaxLength(DeliveryMethod.MaxDeliveryTimeLength);

            deliveryBuilder.OwnsOne(d => d.Price, priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("DeliveryFee")
                    .HasPrecision(18, 2);

                priceBuilder.Property(m => m.Currency)
                    .HasColumnName("DeliveryFeeCurrency")
                    .HasMaxLength(3)
                    .HasConversion(c => c.Code, code => Currency.FromCode(code));
            });
        });

        builder.OwnsOne(o => o.SubTotal, subTotalBuilder =>
        {
            subTotalBuilder.Property(m => m.Amount)
                .HasColumnName("SubTotal")
                .HasPrecision(18, 2);

            subTotalBuilder.Property(m => m.Currency)
                .HasColumnName("SubTotalCurrency")
                .HasMaxLength(3)
                .HasConversion(c => c.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(o => o.ShippingFee, shippingFeeBuilder =>
        {
            shippingFeeBuilder.Property(m => m.Amount)
                .HasColumnName("ShippingFee")
                .HasPrecision(18, 2);

            shippingFeeBuilder.Property(m => m.Currency)
                .HasColumnName("ShippingFeeCurrency")
                .HasMaxLength(3)
                .HasConversion(c => c.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(o => o.Total, totalBuilder =>
        {
            totalBuilder.Property(m => m.Amount)
                .HasColumnName("Total")
                .HasPrecision(18, 2);

            totalBuilder.Property(m => m.Currency)
                .HasColumnName("TotalCurrency")
                .HasMaxLength(3)
                .HasConversion(c => c.Code, code => Currency.FromCode(code));
        });

        // OrderItems are mapped through the private _orderItems backing field. The FK stays
        // a persistence-layer shadow property: items are aggregate-internal children and are
        // only ever created by Order.Create, so the domain must not know about OrderId.
        builder.Navigation(o => o.OrderItems)
            .HasField("_orderItems");

        builder.HasMany(o => o.OrderItems)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
