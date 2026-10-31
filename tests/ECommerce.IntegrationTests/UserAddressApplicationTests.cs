using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.UserAddresses.Commands;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.UserAddresses;
using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.IntegrationTests;

public sealed class UserAddressApplicationTests
{
    [Fact]
    public async Task AddUserAddressCommandHandler_WhenDefaultRequested_ClearsPreviousDefault()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeUserAddressRepository();
        var previousAddress = CreateAddress(userId, "Home", isDefault: true);
        repository.Add(previousAddress);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddUserAddressCommandHandler(repository, new FakeCurrentUserService(userId), unitOfWork);

        var result = await handler.Handle(ValidAddCommand("Work", isDefault: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, repository.Addresses.Count);
        Assert.False(previousAddress.IsDefault);
        Assert.True(repository.Addresses.Single(address => address.Id.Value == result.Value.Id).IsDefault);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateUserAddressCommandHandler_WhenAddressBelongsToAnotherUser_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var repository = new FakeUserAddressRepository();
        var address = CreateAddress(ownerId, "Home");
        repository.Add(address);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateUserAddressCommandHandler(
            repository,
            new FakeCurrentUserService(Guid.NewGuid()),
            unitOfWork);

        var result = await handler.Handle(ValidUpdateCommand(address.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserAddressErrors.NotFound.Code, result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateUserAddressCommandHandler_WhenOwnedAddressUpdated_SavesNewDetails()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeUserAddressRepository();
        var address = CreateAddress(userId, "Home");
        repository.Add(address);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateUserAddressCommandHandler(
            repository,
            new FakeCurrentUserService(userId),
            unitOfWork);

        var result = await handler.Handle(ValidUpdateCommand(address.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Work", result.Value.Label);
        Assert.Equal("Updated Street", result.Value.Street);
        Assert.True(address.IsDefault);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private static UserAddress CreateAddress(Guid userId, string label, bool isDefault = false)
    {
        var address = Address.Create("Jane", "Doe", "+1 555 0100", "US", "Seattle", "1 Main Street", "98101");
        Assert.True(address.IsSuccess);

        var userAddress = UserAddress.Create(UserAddressId.New(), userId, label, address.Value, isDefault);
        Assert.True(userAddress.IsSuccess);
        return userAddress.Value;
    }

    private static AddUserAddressCommand ValidAddCommand(string label, bool isDefault)
        => new(label, isDefault, "Jane", "Doe", "+1 555 0100", "US", "Seattle", "2 Pine Street", "98102");

    private static UpdateUserAddressCommand ValidUpdateCommand(Guid id)
        => new(id, "Work", true, "Jane", "Doe", "+1 555 0100", "US", "Seattle", "Updated Street", "98102");

    private sealed class FakeUserAddressRepository : IUserAddressRepository
    {
        public List<UserAddress> Addresses { get; } = [];

        public Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<UserAddress>>(Addresses.Where(address => address.UserId == userId).ToList());

        public Task<UserAddress?> GetByIdForUserAsync(
            UserAddressId id,
            Guid userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Addresses.FirstOrDefault(address => address.Id == id && address.UserId == userId));

        public void Add(UserAddress userAddress) => Addresses.Add(userAddress);
    }

    private sealed class FakeCurrentUserService(Guid? userId) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
        public string? Email => null;
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
            => action(cancellationToken);
    }
}
