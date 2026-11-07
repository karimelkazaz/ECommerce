using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Auth;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.Entities.UserAddresses;
using MediatR;

namespace ECommerce.IntegrationTests;

// Shared, minimal fakes for outbox-consumer-style tests: inbox claims, outbox
// enqueue capture, policy/email/identity probing. Consumers run inline (no DB
// transaction machinery is exercised here).

internal sealed class FakeInbox(bool claimSucceeds = true) : IInbox
{
    private readonly List<string> _claimsFor = [];

    public int ClaimAttempts { get; private set; }

    public Task<bool> TryClaimAsync(Guid messageId, string eventType, string consumer, CancellationToken ct = default)
    {
        ClaimAttempts++;
        if (claimSucceeds)
        {
            _claimsFor.Add(consumer);
        }

        return Task.FromResult(claimSucceeds);
    }

    public int TotalClaimsFor(string consumer)
        => _claimsFor.Count(c => c == consumer);
}

internal sealed class FakeOutboxWriter : IOutboxWriter
{
    public List<INotification> Messages { get; } = [];

    public void Enqueue(INotification message) => Messages.Add(message);
}

internal sealed class FakeOutboxMessageContext : IOutboxMessageContext
{
    public Guid MessageId { get; } = Guid.NewGuid();

    public string EventType { get; } = "TestEvent";

    public bool IsAvailable => true;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(1);

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
        => action(cancellationToken);
}

internal sealed class FakeInventoryItemRepository : IInventoryItemRepository
{
    private readonly Dictionary<ProductId, InventoryItem> _items = [];

    public IReadOnlyCollection<InventoryItem> Items => _items.Values;

    public Task<InventoryItem?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default)
    {
        _items.TryGetValue(productId, out var item);
        return Task.FromResult(item);
    }

    public Task<List<InventoryItem>> GetByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken ct = default)
        => Task.FromResult(_items.Values.Where(i => productIds.Contains(i.ProductId)).ToList());

    public void Add(InventoryItem inventoryItem) => _items[inventoryItem.ProductId] = inventoryItem;
}

internal sealed class FakeStockAttentionPolicy(
    bool lowStockAlertsEnabled = true,
    int lowStockThreshold = 5) : IStockAttentionPolicy
{
    public bool LowStockAlertsEnabled => lowStockAlertsEnabled;

    public int LowStockThreshold => lowStockThreshold;
}

internal sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string HtmlBody)> Sent { get; } = [];

    public bool ThrowOnSend { get; set; }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        if (ThrowOnSend)
        {
            throw new InvalidOperationException("SMTP is down.");
        }

        Sent.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

internal sealed class FakeIdentityService(UserProfileResponse? profile = null) : IIdentityService
{
    public UserProfileResponse? Profile { get; set; } = profile;

    public Task<bool> UserExistsAsync(string email, CancellationToken ct) => Task.FromResult(true);

    public Task<bool> IsEmailConfirmedAsync(string email, CancellationToken ct) => Task.FromResult(true);

    public Task<Result> CreateUserAsync(string firstName, string lastName, string email, string password, CancellationToken ct)
        => throw new NotSupportedException();

    public Task<Result<AuthResponse>> LoginAsync(string email, string password, CancellationToken ct)
        => throw new NotSupportedException();

    public Task<Result<UserProfileResponse>> GetUserProfileAsync(Guid userId, CancellationToken ct)
        => Profile is null
            ? Task.FromResult(Result.Failure<UserProfileResponse>(Error.NotFound("User.NotFound", "User not found.")))
            : Task.FromResult(Result.Success(Profile));

    public Task<Result<UserProfileResponse>> UpdateUserProfileAsync(Guid userId, string firstName, string lastName, CancellationToken ct)
        => throw new NotSupportedException();

    public Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct)
        => throw new NotSupportedException();

    public Task<Result> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct)
        => throw new NotSupportedException();
}

internal sealed class FakeOrderDbContext : IApplicationDbContext
{
    public readonly List<Order> OrdersList = [];

    public IQueryable<Product> Products => Array.Empty<Product>().AsQueryable().AsTestAsyncQueryable();

    public IQueryable<ProductBrand> Brands => Array.Empty<ProductBrand>().AsQueryable().AsTestAsyncQueryable();

    public IQueryable<ProductType> Types => Array.Empty<ProductType>().AsQueryable().AsTestAsyncQueryable();

    public IQueryable<DeliveryMethod> DeliveryMethods => Array.Empty<DeliveryMethod>().AsQueryable().AsTestAsyncQueryable();

    public IQueryable<Order> Orders => OrdersList.AsQueryable().AsTestAsyncQueryable();

    public IQueryable<InventoryItem> InventoryItems => Array.Empty<InventoryItem>().AsQueryable().AsTestAsyncQueryable();

    public IQueryable<UserAddress> UserAddresses => Array.Empty<UserAddress>().AsQueryable().AsTestAsyncQueryable();

    public IQueryable<SalesSummary> SalesSummaries => Array.Empty<SalesSummary>().AsQueryable().AsTestAsyncQueryable();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(1);

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
        => action(cancellationToken);
}

internal sealed class FakeSalesSummaryRepository : ISalesSummaryRepository
{
    private readonly Dictionary<(DateOnly Day, string CurrencyCode), SalesSummary> _rows = [];

    public IReadOnlyCollection<SalesSummary> Rows => _rows.Values;

    public Task<SalesSummary?> GetByDayAsync(DateOnly day, string currencyCode, CancellationToken ct = default)
    {
        _rows.TryGetValue((day, currencyCode), out var row);
        return Task.FromResult(row);
    }

    public void Add(SalesSummary salesSummary)
        => _rows[(salesSummary.Day, salesSummary.CurrencyCode)] = salesSummary;
}
