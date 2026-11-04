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

        builder.Property(e => e.OrderId)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .IsRequired();

        builder.Property(e => e.Type);

        builder.Property(e => e.Quantity)
            .IsRequired();

        builder.HasIndex(e => e.OrderId);
    }
}
