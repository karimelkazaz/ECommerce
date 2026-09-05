using ECommerce.Infrastructure.Persistence.Otp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("OtpCodes", "identity");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(o => o.Purpose)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.CodeHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(o => new { o.Email, o.Purpose });
    }
}