using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        BaseEntityConfiguration.Configure<InventoryItem, InventoryItemId>(builder);

        builder.ToTable("InventoryItems");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(
                id => id.Value,
                value => new InventoryItemId(value));

        builder.Property(i => i.ProductId)
            .HasConversion(
                id => id.Value,
                value => new ProductId(value));

        builder.Property(i => i.AvailableQuantity)
            .IsRequired();

        // Optimistic concurrency: rowversion column, tracked as a concurrency
        // token. A concurrent stock mutation fails at save time rather than
        // silently last-write-wins-ing over another worker's change.
        builder.Property(i => i.RowVersion)
            .IsRowVersion();

        // One inventory record per product. Soft-deleted products keep their
        // row (NoAction), mirroring how Products reference brands and types.
        builder.HasIndex(i => i.ProductId)
            .IsUnique();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        // Ledger entries are aggregate-internal children mapped through the
        // private _ledger backing field; the FK stays a shadow property so the
        // domain does not know about InventoryItemId.
        builder.Navigation(i => i.Ledger)
            .HasField("_ledger");

        builder.HasMany(i => i.Ledger)
            .WithOne()
            .HasForeignKey("InventoryItemId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
