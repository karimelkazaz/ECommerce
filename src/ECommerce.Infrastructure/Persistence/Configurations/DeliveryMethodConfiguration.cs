using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations
{
    public sealed class DeliveryMethodConfiguration
    : IEntityTypeConfiguration<DeliveryMethod>
    {
        public void Configure(
            EntityTypeBuilder<DeliveryMethod> builder)
        {
            BaseEntityConfiguration.Configure<DeliveryMethod, DeliveryMethodId>(builder);

            builder.ToTable("DeliveryMethods");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new DeliveryMethodId(value));

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(DeliveryMethod.MaxNameLength);

            builder.Property(x => x.DeliveryTime)
                .IsRequired()
                .HasMaxLength(DeliveryMethod.MaxDeliveryTimeLength);

            builder.OwnsOne(x => x.Price, priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("Price")
                    .HasPrecision(18, 2);

                priceBuilder.Property(m => m.Currency)
                    .HasMaxLength(3)
                    .HasConversion(c => c.Code, code => Currency.FromCode(code));
            });

            builder.HasIndex(x => x.Name)
                .IsUnique();
        }
    }
}
