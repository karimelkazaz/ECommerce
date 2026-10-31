namespace ECommerce.Domain.Entities.UserAddresses;

public interface IUserAddressRepository
{
    Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserAddress?> GetByIdForUserAsync(
        UserAddressId id,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(UserAddress userAddress);
}