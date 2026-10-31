using ECommerce.Domain.Entities.UserAddresses;

namespace ECommerce.Application.UserAddresses;

public sealed record UserAddressResponse(
    Guid Id,
    string Label,
    bool IsDefault,
    string RecipientFirstName,
    string RecipientLastName,
    string PhoneNumber,
    string Country,
    string City,
    string Street,
    string PostalCode)
{
    public static UserAddressResponse FromDomain(UserAddress userAddress)
        => new(
            userAddress.Id.Value,
            userAddress.Label,
            userAddress.IsDefault,
            userAddress.Address.RecipientFirstName,
            userAddress.Address.RecipientLastName,
            userAddress.Address.PhoneNumber,
            userAddress.Address.Country,
            userAddress.Address.City,
            userAddress.Address.Street,
            userAddress.Address.PostalCode);
}