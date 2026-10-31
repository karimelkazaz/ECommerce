using ECommerce.Application.DeliveryMethods.Commands;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.IntegrationTests;

public class DeliveryMethodApplicationTests
{
    private readonly FakeDeliveryMethodRepository _deliveryMethods = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static DeliveryMethod Seed(
        string name = "Standard Delivery",
        string deliveryTime = "3-5 business days",
        decimal price = 30m)
        => DeliveryMethod.Create(DeliveryMethodId.New(), name, deliveryTime, Money.Create(price, Currency.Usd).Value).Value;

    [Fact]
    public async Task CreateDeliveryMethodCommandHandler_WithValidCommand_CreatesAndReturnsResponse()
    {
        var handler = new CreateDeliveryMethodCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new CreateDeliveryMethodCommand("Express", "1-2 days", 60m, "USD"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Express", result.Value.Name);
        Assert.Equal("1-2 days", result.Value.DeliveryTime);
        Assert.Equal(60m, result.Value.Price);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Null(result.Value.Description);
        Assert.True(result.Value.IsAvailable);
        Assert.Single(_deliveryMethods.Items);
    }

    [Fact]
    public async Task CreateDeliveryMethodCommandHandler_WithDescription_ReturnsDescriptionAndAvailability()
    {
        var handler = new CreateDeliveryMethodCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new CreateDeliveryMethodCommand("Express", "1-2 days", 60m, "USD", "  Priority handling  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Priority handling", result.Value.Description);
        Assert.True(result.Value.IsAvailable);
    }

    [Fact]
    public async Task CreateDeliveryMethodCommandHandler_WithInvalidCurrency_Fails()
    {
        var handler = new CreateDeliveryMethodCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new CreateDeliveryMethodCommand("Express", "1-2 days", 60m, "XXX"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Money.InvalidCurrency", result.Error.Code);
        Assert.Empty(_deliveryMethods.Items);
    }

    [Fact]
    public async Task CreateDeliveryMethodCommandHandler_WithNegativePrice_Fails()
    {
        var handler = new CreateDeliveryMethodCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new CreateDeliveryMethodCommand("Express", "1-2 days", -1m, "USD"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Money.NegativeAmount", result.Error.Code);
        Assert.Empty(_deliveryMethods.Items);
    }

    [Fact]
    public async Task RenameDeliveryMethodCommandHandler_WithValidCommand_UpdatesName()
    {
        var deliveryMethod = Seed();
        _deliveryMethods.Add(deliveryMethod);

        var handler = new RenameDeliveryMethodCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new RenameDeliveryMethodCommand(deliveryMethod.Id.Value, "Same Day"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Same Day", deliveryMethod.Name);
    }

    [Fact]
    public async Task RenameDeliveryMethodCommandHandler_WhenNotFound_Fails()
    {
        var handler = new RenameDeliveryMethodCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new RenameDeliveryMethodCommand(Guid.NewGuid(), "Same Day"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ChangeDeliveryMethodDeliveryTimeCommandHandler_WithValidCommand_UpdatesDeliveryTime()
    {
        var deliveryMethod = Seed();
        _deliveryMethods.Add(deliveryMethod);

        var handler = new ChangeDeliveryMethodDeliveryTimeCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodDeliveryTimeCommand(deliveryMethod.Id.Value, "Within 6 hours"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Within 6 hours", deliveryMethod.DeliveryTime);
    }

    [Fact]
    public async Task ChangeDeliveryMethodDeliveryTimeCommandHandler_WhenNotFound_Fails()
    {
        var handler = new ChangeDeliveryMethodDeliveryTimeCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodDeliveryTimeCommand(Guid.NewGuid(), "Within 6 hours"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ChangeDeliveryMethodPriceCommandHandler_WithValidCommand_UpdatesPrice()
    {
        var deliveryMethod = Seed();
        _deliveryMethods.Add(deliveryMethod);

        var handler = new ChangeDeliveryMethodPriceCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodPriceCommand(deliveryMethod.Id.Value, 75m, "EGP"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(75m, deliveryMethod.Price.Amount);
        Assert.Equal(Currency.Egp, deliveryMethod.Price.Currency);
    }

    [Fact]
    public async Task ChangeDeliveryMethodPriceCommandHandler_WithInvalidCurrency_Fails()
    {
        var handler = new ChangeDeliveryMethodPriceCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodPriceCommand(Guid.NewGuid(), 75m, "XXX"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Money.InvalidCurrency", result.Error.Code);
    }

    [Fact]
    public async Task ChangeDeliveryMethodPriceCommandHandler_WhenNotFound_Fails()
    {
        var handler = new ChangeDeliveryMethodPriceCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodPriceCommand(Guid.NewGuid(), 75m, "USD"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task DeleteDeliveryMethodCommandHandler_WithExistingMethod_RemovesIt()
    {
        var deliveryMethod = Seed();
        _deliveryMethods.Add(deliveryMethod);

        var handler = new DeleteDeliveryMethodCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new DeleteDeliveryMethodCommand(deliveryMethod.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_deliveryMethods.Items);
    }

    [Fact]
    public async Task DeleteDeliveryMethodCommandHandler_WhenNotFound_Fails()
    {
        var handler = new DeleteDeliveryMethodCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new DeleteDeliveryMethodCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ChangeDeliveryMethodDescriptionCommandHandler_WithValidCommand_UpdatesDescription()
    {
        var deliveryMethod = Seed();
        _deliveryMethods.Add(deliveryMethod);

        var handler = new ChangeDeliveryMethodDescriptionCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodDescriptionCommand(deliveryMethod.Id.Value, "  Courier pickup  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Courier pickup", deliveryMethod.Description);
    }

    [Fact]
    public async Task ChangeDeliveryMethodDescriptionCommandHandler_WithBlank_ClearsDescription()
    {
        var deliveryMethod = Seed();
        deliveryMethod.ChangeDescription("Courier pickup");
        _deliveryMethods.Add(deliveryMethod);

        var handler = new ChangeDeliveryMethodDescriptionCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodDescriptionCommand(deliveryMethod.Id.Value, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(deliveryMethod.Description);
    }

    [Fact]
    public async Task ChangeDeliveryMethodDescriptionCommandHandler_WhenNotFound_Fails()
    {
        var handler = new ChangeDeliveryMethodDescriptionCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodDescriptionCommand(Guid.NewGuid(), "Courier pickup"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ChangeDeliveryMethodAvailabilityCommandHandler_WithValidCommand_UpdatesAvailability()
    {
        var deliveryMethod = Seed();
        _deliveryMethods.Add(deliveryMethod);

        var handler = new ChangeDeliveryMethodAvailabilityCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodAvailabilityCommand(deliveryMethod.Id.Value, false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(deliveryMethod.IsAvailable);
    }

    [Fact]
    public async Task ChangeDeliveryMethodAvailabilityCommandHandler_WhenNotFound_Fails()
    {
        var handler = new ChangeDeliveryMethodAvailabilityCommandHandler(_deliveryMethods, _unitOfWork);

        var result = await handler.Handle(
            new ChangeDeliveryMethodAvailabilityCommand(Guid.NewGuid(), false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public void CreateDeliveryMethodCommandValidator_ValidatesNameDeliveryTimePriceAndCurrency()
    {
        var validator = new CreateDeliveryMethodCommandValidator();

        Assert.False(validator.Validate(new CreateDeliveryMethodCommand("", "1-2 days", 10m, "USD")).IsValid);
        Assert.False(validator.Validate(new CreateDeliveryMethodCommand("Express", "", 10m, "USD")).IsValid);
        Assert.False(validator.Validate(new CreateDeliveryMethodCommand("Express", "1-2 days", -1m, "USD")).IsValid);
        Assert.False(validator.Validate(new CreateDeliveryMethodCommand("Express", "1-2 days", 10m, "XXX")).IsValid);

        Assert.True(validator.Validate(new CreateDeliveryMethodCommand("Express", "1-2 days", 0m, "EGP")).IsValid);
    }

    [Fact]
    public void RenameDeliveryMethodCommandValidator_ValidatesIdAndName()
    {
        var validator = new RenameDeliveryMethodCommandValidator();

        Assert.False(validator.Validate(new RenameDeliveryMethodCommand(Guid.Empty, "Express")).IsValid);
        Assert.False(validator.Validate(new RenameDeliveryMethodCommand(Guid.NewGuid(), "")).IsValid);

        Assert.True(validator.Validate(new RenameDeliveryMethodCommand(Guid.NewGuid(), "Express")).IsValid);
    }

    [Fact]
    public void ChangeDeliveryMethodDeliveryTimeCommandValidator_ValidatesIdAndDeliveryTime()
    {
        var validator = new ChangeDeliveryMethodDeliveryTimeCommandValidator();

        Assert.False(validator.Validate(new ChangeDeliveryMethodDeliveryTimeCommand(Guid.Empty, "1-2 days")).IsValid);
        Assert.False(validator.Validate(new ChangeDeliveryMethodDeliveryTimeCommand(Guid.NewGuid(), "")).IsValid);

        Assert.True(validator.Validate(new ChangeDeliveryMethodDeliveryTimeCommand(Guid.NewGuid(), "1-2 days")).IsValid);
    }

    [Fact]
    public void ChangeDeliveryMethodPriceCommandValidator_ValidatesIdAmountAndCurrency()
    {
        var validator = new ChangeDeliveryMethodPriceCommandValidator();

        Assert.False(validator.Validate(new ChangeDeliveryMethodPriceCommand(Guid.Empty, 10m, "USD")).IsValid);
        Assert.False(validator.Validate(new ChangeDeliveryMethodPriceCommand(Guid.NewGuid(), -1m, "USD")).IsValid);
        Assert.False(validator.Validate(new ChangeDeliveryMethodPriceCommand(Guid.NewGuid(), 10m, "XXX")).IsValid);

        Assert.True(validator.Validate(new ChangeDeliveryMethodPriceCommand(Guid.NewGuid(), 0m, "USD")).IsValid);
    }

    [Fact]
    public void DeleteDeliveryMethodCommandValidator_ValidatesId()
    {
        var validator = new DeleteDeliveryMethodCommandValidator();

        Assert.False(validator.Validate(new DeleteDeliveryMethodCommand(Guid.Empty)).IsValid);
        Assert.True(validator.Validate(new DeleteDeliveryMethodCommand(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void ChangeDeliveryMethodDescriptionCommandValidator_ValidatesIdAndDescription()
    {
        var validator = new ChangeDeliveryMethodDescriptionCommandValidator();

        Assert.False(validator.Validate(new ChangeDeliveryMethodDescriptionCommand(Guid.Empty, "Courier pickup")).IsValid);
        Assert.False(validator.Validate(new ChangeDeliveryMethodDescriptionCommand(Guid.NewGuid(), new string('a', DeliveryMethod.MaxDescriptionLength + 1))).IsValid);

        Assert.True(validator.Validate(new ChangeDeliveryMethodDescriptionCommand(Guid.NewGuid(), "Courier pickup")).IsValid);
        Assert.True(validator.Validate(new ChangeDeliveryMethodDescriptionCommand(Guid.NewGuid(), null)).IsValid);
    }

    [Fact]
    public void ChangeDeliveryMethodAvailabilityCommandValidator_ValidatesId()
    {
        var validator = new ChangeDeliveryMethodAvailabilityCommandValidator();

        Assert.False(validator.Validate(new ChangeDeliveryMethodAvailabilityCommand(Guid.Empty, false)).IsValid);
        Assert.True(validator.Validate(new ChangeDeliveryMethodAvailabilityCommand(Guid.NewGuid(), false)).IsValid);
        Assert.True(validator.Validate(new ChangeDeliveryMethodAvailabilityCommand(Guid.NewGuid(), true)).IsValid);
    }

    private sealed class FakeDeliveryMethodRepository : IDeliveryMethodRepository
    {
        private readonly Dictionary<DeliveryMethodId, DeliveryMethod> _deliveryMethods = new();

        public IReadOnlyCollection<DeliveryMethod> Items => _deliveryMethods.Values;

        public Task<DeliveryMethod?> GetByIdAsync(DeliveryMethodId id, CancellationToken ct = default)
        {
            _deliveryMethods.TryGetValue(id, out var deliveryMethod);
            return Task.FromResult(deliveryMethod);
        }

        public void Add(DeliveryMethod deliveryMethod) => _deliveryMethods[deliveryMethod.Id] = deliveryMethod;

        public void Remove(DeliveryMethod deliveryMethod) => _deliveryMethods.Remove(deliveryMethod.Id);
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
