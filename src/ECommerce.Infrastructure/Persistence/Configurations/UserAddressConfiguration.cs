using ECommerce.Domain.Entities.UserAddresses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.ToTable("UserAddresses", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new UserAddressId(value));

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(UserAddress.MaxLabelLength);

        builder.Property(x => x.IsDefault)
            .IsRequired();

        builder.OwnsOne(x => x.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.RecipientFirstName)
                .HasColumnName("RecipientFirstName")
                .IsRequired()
                .HasMaxLength(100);

            addressBuilder.Property(a => a.RecipientLastName)
                .HasColumnName("RecipientLastName")
                .IsRequired()
                .HasMaxLength(100);

            addressBuilder.Property(a => a.PhoneNumber)
                .HasColumnName("PhoneNumber")
                .IsRequired()
                .HasMaxLength(20);

            addressBuilder.Property(a => a.Country)
                .HasColumnName("Country")
                .IsRequired()
                .HasMaxLength(100);

            addressBuilder.Property(a => a.City)
                .HasColumnName("City")
                .IsRequired()
                .HasMaxLength(100);

            addressBuilder.Property(a => a.Street)
                .HasColumnName("Street")
                .IsRequired()
                .HasMaxLength(200);

            addressBuilder.Property(a => a.PostalCode)
                .HasColumnName("PostalCode")
                .IsRequired()
                .HasMaxLength(20);
        });

        builder.HasIndex(x => x.UserId);
    }
}
