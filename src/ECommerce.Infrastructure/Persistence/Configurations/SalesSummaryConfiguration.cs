using ECommerce.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class SalesSummaryConfiguration : IEntityTypeConfiguration<SalesSummary>
{
    public void Configure(EntityTypeBuilder<SalesSummary> builder)
    {
        BaseEntityConfiguration.Configure<SalesSummary, Guid>(builder);

        builder.ToTable("SalesSummaries");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.Day)
            .IsRequired();

        builder.Property(s => s.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(s => s.Revenue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.OrderCount)
            .IsRequired();

        builder.HasIndex(s => new { s.Day, s.CurrencyCode })
            .IsUnique();
    }
}
