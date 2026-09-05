using ECommerce.Domain.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.UserAddresses;

public class UserAddress : Entity<UserAddressId>
{
    public const int MaxLabelLength = 50;

    public Guid UserId { get; private set; }
    public string Label { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public Address Address { get; private set; } = null!;

    private UserAddress() { }

    private UserAddress(
        UserAddressId id,
        Guid userId,
        string label,
        Address address,
        bool isDefault) : base(id)
    {
        UserId = userId;
        Label = label;
        Address = address;
        IsDefault = isDefault;
    }

    public static Result<UserAddress> Create(
        UserAddressId id,
        Guid userId,
        string label,
        Address address,
        bool isDefault = false)
    {
        if (id.Value == Guid.Empty)
            return Result.Failure<UserAddress>(UserAddressErrors.InvalidId);

        if (userId == Guid.Empty)
            return Result.Failure<UserAddress>(UserAddressErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(label))
            return Result.Failure<UserAddress>(UserAddressErrors.LabelRequired);

        if (label.Length > MaxLabelLength)
            return Result.Failure<UserAddress>(UserAddressErrors.LabelTooLong);

        if (address is null)
            return Result.Failure<UserAddress>(UserAddressErrors.AddressRequired);

        return Result.Success(new UserAddress(
            id,
            userId,
            label.Trim(),
            address,
            isDefault));
    }

    public Result UpdateDetails(string label, Address address)
    {
        if (string.IsNullOrWhiteSpace(label))
            return Result.Failure(UserAddressErrors.LabelRequired);

        if (label.Length > MaxLabelLength)
            return Result.Failure(UserAddressErrors.LabelTooLong);

        if (address is null)
            return Result.Failure(UserAddressErrors.AddressRequired);

        Label = label.Trim();
        Address = address;
        return Result.Success();
    }

    public void SetDefault(bool isDefault) => IsDefault = isDefault;
}
