using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.UserAddresses;

public static class UserAddressErrors
{
    public static readonly Error NotFound = Error.NotFound("UserAddress.NotFound", "User address not found.");
    public static readonly Error InvalidId = Error.Validation("UserAddress.InvalidId", "User address ID is required.");
    public static readonly Error InvalidUserId = Error.Validation("UserAddress.InvalidUserId", "User ID is required.");
    public static readonly Error LabelRequired = Error.Validation("UserAddress.LabelRequired", "Label is required.");
    public static readonly Error LabelTooLong = Error.Validation("UserAddress.LabelTooLong", $"Label cannot exceed {UserAddress.MaxLabelLength} characters.");
    public static readonly Error AddressRequired = Error.Validation("UserAddress.AddressRequired", "Address details are required.");
}
