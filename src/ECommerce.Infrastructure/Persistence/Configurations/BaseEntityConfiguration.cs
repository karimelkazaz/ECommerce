using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

internal static class BaseEntityConfiguration
{
    public static void Configure<TEntity, TId>(EntityTypeBuilder<TEntity> builder)
        where TEntity : Entity<TId>
        where TId : notnull
    {
        builder.Property(entity => entity.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}