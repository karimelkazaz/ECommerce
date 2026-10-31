using ECommerce.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(m => new { m.MessageId, m.Consumer });

        builder.Property(m => m.Consumer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.EventType)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(m => m.ConsumedOnUtc);
    }
}
