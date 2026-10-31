using ECommerce.Domain.Entities.UserAddresses;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class UserAddressRepository(ApplicationDbContext dbContext) : IUserAddressRepository
{
    public async Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await dbContext.UserAddresses
            .Where(address => address.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<UserAddress?> GetByIdForUserAsync(
        UserAddressId id,
        Guid userId,
        CancellationToken cancellationToken = default)
        => dbContext.UserAddresses
            .FirstOrDefaultAsync(address => address.Id == id && address.UserId == userId, cancellationToken);

    public void Add(UserAddress userAddress)
        => dbContext.UserAddresses.Add(userAddress);
}