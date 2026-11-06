using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Inventory;
using ECommerce.Application.Inventory.Commands;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.IntegrationTests;

public class InventoryApplicationTests
{
    private readonly FakeProductRepository _products = new();
    private readonly FakeInventoryItemRepository _inventory = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task CreateInventoryItemCommandHandler_WithValidCommand_CreatesRecord()
    {
        var product = SeedProduct();
        _products.Add(product);

        var handler = new CreateInventoryItemCommandHandler(_products, _inventory, _unitOfWork);

        var result = await handler.Handle(
            new CreateInventoryItemCommand(product.Id.Value, 15),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(product.Id.Value, result.Value.ProductId);
        Assert.Equal(15, result.Value.AvailableQuantity);
        Assert.Single(_inventory.Items);

        var created = _inventory.Items.Single();
        var initialEntry = Assert.Single(created.Ledger);
        Assert.Equal(StockLedgerEntryType.Initial, initialEntry.Type);
        Assert.Equal(15, initialEntry.Quantity);
        Assert.Null(initialEntry.OrderId);
    }

    [Fact]
    public async Task CreateInventoryItemCommandHandler_WithZeroQuantity_CreatesOutOfStockRecord()
    {
        var product = SeedProduct();
        _products.Add(product);

        var handler = new CreateInventoryItemCommandHandler(_products, _inventory, _unitOfWork);

        var result = await handler.Handle(
            new CreateInventoryItemCommand(product.Id.Value, 0),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.AvailableQuantity);
        Assert.Empty(_inventory.Items.Single().Ledger);
    }

    [Fact]
    public async Task CreateInventoryItemCommandHandler_UnknownProduct_Fails()
    {
        var handler = new CreateInventoryItemCommandHandler(_products, _inventory, _unitOfWork);

        var result = await handler.Handle(
            new CreateInventoryItemCommand(Guid.NewGuid(), 5),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.NotFound.Code, result.Error.Code);
        Assert.Empty(_inventory.Items);
    }

    [Fact]
    public async Task CreateInventoryItemCommandHandler_DuplicateRecordForProduct_Fails()
    {
        var product = SeedProduct();
        _products.Add(product);
        _inventory.Add(InventoryItem.Create(product.Id, 3).Value);

        var handler = new CreateInventoryItemCommandHandler(_products, _inventory, _unitOfWork);

        var result = await handler.Handle(
            new CreateInventoryItemCommand(product.Id.Value, 5),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.Duplicate.Code, result.Error.Code);
    }

    [Fact]
    public async Task AdjustInventoryItemCommandHandler_WithPositiveDelta_IncreasesStock()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;
        _inventory.Add(inventoryItem);

        var handler = new AdjustInventoryItemCommandHandler(_inventory, _unitOfWork);

        var result = await handler.Handle(
            new AdjustInventoryItemCommand(inventoryItem.ProductId.Value, 7),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(17, inventoryItem.AvailableQuantity);

        var adjustment = Assert.Single(inventoryItem.Ledger, e => e.Type == StockLedgerEntryType.Adjustment);
        Assert.Equal(StockLedgerEntryType.Adjustment, adjustment.Type);
        Assert.Equal(7, adjustment.Quantity);
        Assert.Null(adjustment.OrderId);
        Assert.Equal(inventoryItem.AvailableQuantity, inventoryItem.ReconstructedAvailability());
    }

    [Fact]
    public async Task AdjustInventoryItemCommandHandler_WithNegativeDelta_DecreasesStock()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;
        _inventory.Add(inventoryItem);

        var handler = new AdjustInventoryItemCommandHandler(_inventory, _unitOfWork);

        var result = await handler.Handle(
            new AdjustInventoryItemCommand(inventoryItem.ProductId.Value, -4),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, inventoryItem.AvailableQuantity);

        var adjustment = Assert.Single(inventoryItem.Ledger, e => e.Type == StockLedgerEntryType.Adjustment);
        Assert.Equal(StockLedgerEntryType.Adjustment, adjustment.Type);
        Assert.Equal(-4, adjustment.Quantity);
        Assert.Null(adjustment.OrderId);
    }

    [Fact]
    public async Task AdjustInventoryItemCommandHandler_UnknownProduct_Fails()
    {
        var handler = new AdjustInventoryItemCommandHandler(_inventory, _unitOfWork);

        var result = await handler.Handle(
            new AdjustInventoryItemCommand(Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task AdjustInventoryItemCommandHandler_BelowZero_Fails()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 3).Value;
        _inventory.Add(inventoryItem);

        var handler = new AdjustInventoryItemCommandHandler(_inventory, _unitOfWork);

        var result = await handler.Handle(
            new AdjustInventoryItemCommand(inventoryItem.ProductId.Value, -4),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.InsufficientStock.Code, result.Error.Code);
        Assert.Equal(3, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public void CreateInventoryItemCommandValidator_ValidatesProductIdAndQuantity()
    {
        var validator = new CreateInventoryItemCommandValidator();

        Assert.False(validator.Validate(new CreateInventoryItemCommand(Guid.Empty, 5)).IsValid);
        Assert.False(validator.Validate(new CreateInventoryItemCommand(Guid.NewGuid(), -1)).IsValid);

        Assert.True(validator.Validate(new CreateInventoryItemCommand(Guid.NewGuid(), 0)).IsValid);
        Assert.True(validator.Validate(new CreateInventoryItemCommand(Guid.NewGuid(), 5)).IsValid);
    }

    [Fact]
    public void AdjustInventoryItemCommandValidator_ValidatesProductIdAndNonZeroDelta()
    {
        var validator = new AdjustInventoryItemCommandValidator();

        Assert.False(validator.Validate(new AdjustInventoryItemCommand(Guid.Empty, 1)).IsValid);
        Assert.False(validator.Validate(new AdjustInventoryItemCommand(Guid.NewGuid(), 0)).IsValid);

        Assert.True(validator.Validate(new AdjustInventoryItemCommand(Guid.NewGuid(), 1)).IsValid);
        Assert.True(validator.Validate(new AdjustInventoryItemCommand(Guid.NewGuid(), -3)).IsValid);
    }

    private static Product SeedProduct()
        => Product.Create(
            ProductId.New(),
            "Classic Tee",
            "A comfortable t-shirt.",
            "https://img.example.com/tee.png",
            Money.Create(20m, Currency.Usd).Value).Value;

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<ProductId, Product> _products = [];

        public IReadOnlyCollection<Product> Items => _products.Values;

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        {
            _products.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }

        public void Add(Product product) => _products[product.Id] = product;

        public void Remove(Product product) => _products.Remove(product.Id);
    }

    private sealed class FakeInventoryItemRepository : IInventoryItemRepository
    {
        private readonly Dictionary<ProductId, InventoryItem> _items = [];

        public IReadOnlyCollection<InventoryItem> Items => _items.Values;

        public Task<InventoryItem?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default)
        {
            _items.TryGetValue(productId, out var inventoryItem);
            return Task.FromResult(inventoryItem);
        }

        public Task<List<InventoryItem>> GetByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken ct = default)
            => Task.FromResult(_items.Values.Where(i => productIds.Contains(i.ProductId)).ToList());

        public void Add(InventoryItem inventoryItem) => _items[inventoryItem.ProductId] = inventoryItem;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
            => action(cancellationToken);
    }
}
