using ECommerce.Application;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence.DbContexts;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Persistence-level verification for the Order → OrderItem relationship:
/// a single required shadow FK on OrderItems, aggregate-only item management,
/// and an FK constraint that rejects orphaned items in the database itself.
/// </summary>
public sealed class OrderPersistenceTests : IAsyncLifetime
{
    private const string ServerConnectionString =
        "Server=.;Trusted_Connection=True;TrustServerCertificate=True;";

    private static readonly string DatabaseName = $"ECommerce_FK_{Guid.NewGuid():N}";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    $"Server=.;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;",
                ["Jwt:Issuer"] = "ECommerce.Test",
                ["Jwt:Audience"] = "ECommerce.Test",
                ["Jwt:SecretKey"] =
                    "test-secret-key-that-is-at-least-32-characters-long-0123456789",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        _provider = services.BuildServiceProvider();

        await using var scope = _provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();

        await using var connection = new SqlConnection(ServerConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public void OrderItemRelationship_IsRequiredSingleShadowForeignKey()
    {
        using var context = CreateContext();

        var itemEntityType = context.Model.FindEntityType(typeof(OrderItem))!;
        var foreignKeys = itemEntityType.GetForeignKeys().ToList();

        // Exactly one relationship between OrderItem and Order — no duplicate or optional one.
        var foreignKey = Assert.Single(foreignKeys);

        Assert.True(foreignKey.IsRequired);
        Assert.Equal(typeof(Order), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        Assert.Equal(nameof(Order.OrderItems), foreignKey.PrincipalToDependent!.Name);

        var foreignKeyProperty = Assert.Single(foreignKey.Properties);
        Assert.Equal("OrderId", foreignKeyProperty.Name);
        Assert.False(foreignKeyProperty.IsNullable);

        // The FK belongs to the persistence layer: no CLR member backs it.
        Assert.Null(foreignKeyProperty.PropertyInfo);
        Assert.Null(foreignKeyProperty.FieldInfo);

        // The item collection is materialized into the aggregate's private field,
        // so items can never be attached from outside the Order aggregate root.
        Assert.Equal("_orderItems", foreignKey.PrincipalToDependent!.FieldInfo!.Name);
    }

    [Fact]
    public async Task Order_SaveAndReload_CarriesItemsThroughTheAggregate()
    {
        var order = CreateOrderWithTwoItems();

        Guid savedOrderId;
        await using (var arrangeScope = _provider.CreateAsyncScope())
        {
            var dbContext = arrangeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.OrdersSet.Add(order);
            await dbContext.SaveChangesAsync();

            savedOrderId = order.Id.Value;
        }

        await using var assertScope = _provider.CreateAsyncScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var reloaded = await assertContext.OrdersSet
            .Include(o => o.OrderItems)
            .SingleAsync(o => o.Id == new OrderId(savedOrderId));

        Assert.Equal(2, reloaded.OrderItems.Count);

        // EF propagated the required shadow FK from the aggregate on save; every stored
        // item row belongs to exactly this order.
        foreach (var itemEntry in assertContext.ChangeTracker.Entries<OrderItem>())
        {
            var shadowOrderId = Assert.IsType<OrderId>(itemEntry.Property("OrderId").CurrentValue).Value;
            Assert.Equal(savedOrderId, shadowOrderId);
        }
    }

    [Fact]
    public async Task OrderItem_WithForeignOrderId_CannotBePersisted()
    {
        var order = CreateOrderWithTwoItems();

        await using var scope = _provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.OrdersSet.Add(order);
        await dbContext.SaveChangesAsync();

        // Corrupt the shadow FK of one item to point at a non-existent order; the
        // database (not the domain — which cannot produce this shape) must reject it.
        var itemEntry = dbContext.Entry(order.OrderItems.Single(i => i.Quantity == 2));
        itemEntry.Property("OrderId").CurrentValue = new OrderId(Guid.NewGuid());

        var updateException = await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            dbContext.SaveChangesAsync());

        Assert.IsAssignableFrom<SqlException>(updateException.InnerException);
    }

    private static Order CreateOrderWithTwoItems()
    {
        var addressResult = Address.Create(
            "John", "Doe", "+20 100 000 0000", "Egypt", "Cairo", "1 Main Street", "11511");
        Assert.True(addressResult.IsSuccess);

        var deliveryFeeResult = Money.Create(5m, Currency.Usd);
        Assert.True(deliveryFeeResult.IsSuccess);

        var deliveryMethodResult = DeliveryMethod.Create(
            DeliveryMethodId.New(), "Standard", "3-5 business days", deliveryFeeResult.Value);
        Assert.True(deliveryMethodResult.IsSuccess);

        var firstPrice = Money.Create(20m, Currency.Usd);
        var secondPrice = Money.Create(30m, Currency.Usd);
        Assert.True(firstPrice.IsSuccess);
        Assert.True(secondPrice.IsSuccess);

        var orderResult = Order.Create(
            OrderId.New(),
            Guid.NewGuid(),
            addressResult.Value,
            [
                new BasketItemDto(
                    new ProductId(Guid.NewGuid()), "Classic Tee", "https://img.example.com/tee.png", firstPrice.Value, 1),
                new BasketItemDto(
                    new ProductId(Guid.NewGuid()), "Classic Mug", "https://img.example.com/mug.png", secondPrice.Value, 2),
            ],
            OrderDeliveryMethod.FromEntity(deliveryMethodResult.Value));

        Assert.True(orderResult.IsSuccess);
        return orderResult.Value;
    }

    private static ApplicationDbContext CreateContext()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ServerConnectionString)
            .Options);
}
