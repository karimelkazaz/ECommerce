using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public static class JsonSeeder
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task SeedIfEmpty<TEntity, TModel>(
            DbSet<TEntity> dbSet,
            string fileName,
            Func<TModel, TEntity> map,
            ILogger? logger = null,
            CancellationToken ct = default
        ) where TEntity : class
    {
        if (await dbSet.AnyAsync(ct))
            return;

        var filePath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeding", "Data", fileName);

        if (!File.Exists(filePath))
        {
            logger?.LogWarning("Seed data file not found: {FilePath}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);

        var models = await JsonSerializer.DeserializeAsync<List<TModel>>(stream, options, ct);

        if (models is null || models.Count == 0)
        {
            logger?.LogWarning("Seed data file '{FileName}' contains no records.", fileName);
            return;
        }

        await dbSet.AddRangeAsync(models.Select(map), ct);
    }
}
