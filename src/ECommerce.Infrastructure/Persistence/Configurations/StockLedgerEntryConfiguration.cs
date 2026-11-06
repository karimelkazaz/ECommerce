using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class StockLedgerEntryConfiguration : IEntityTypeConfiguration<StockLedgerEntry>
{
    public void Configure(EntityTypeBuilder<StockLedgerEntry> builder)
    {
        BaseEntityConfiguration.Configure<StockLedgerEntry, Guid>(builder);

        builder.ToTable("StockLedgerEntries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // Null for Initial/Adjustment entries; order-bound only for Reserve/Release.
        builder.Property(e => e.OrderId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new OrderId(value.Value) : (OrderId?)null);

        // Stored as its enum name ('Reserve', 'Release', ...) so raw SQL queries
        // and database logs are human-readable without joining enum definitions.
        builder.Property(e => e.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Quantity)
            .IsRequired();

        builder.HasIndex(e => new { e.OrderId, e.Type });
    }
}
