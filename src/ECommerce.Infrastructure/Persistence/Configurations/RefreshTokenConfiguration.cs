using ECommerce.Domain.Entities.RefreshTokens;
using ECommerce.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        BaseEntityConfiguration.Configure<RefreshToken, RefreshTokenId>(builder);

        builder.ToTable("RefreshTokens", "identity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new RefreshTokenId(value));

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(RefreshToken.MaxTokenHashLength);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresOnUtc)
            .IsRequired();

        builder.Property(x => x.RevokedOnUtc);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => x.UserId);
    }
}