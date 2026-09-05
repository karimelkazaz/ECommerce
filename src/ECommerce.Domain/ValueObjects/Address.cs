using ECommerce.Domain.Common;

namespace ECommerce.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string RecipientFirstName { get; private set; } = null!;
    public string RecipientLastName { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Street { get; private set; } = null!;
    public string PostalCode { get; private set; } = null!;

    private Address() { }

    private Address(
        string recipientFirstName,
        string recipientLastName,
        string phoneNumber,
        string country,
        string city,
        string street,
        string postalCode)
    {
        RecipientFirstName = recipientFirstName;
        RecipientLastName = recipientLastName;
        PhoneNumber = phoneNumber;
        Country = country;
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    public static Result<Address> Create(
        string recipientFirstName,
        string recipientLastName,
        string phoneNumber,
        string country,
        string city,
        string street,
        string postalCode)
    {
        if (string.IsNullOrWhiteSpace(recipientFirstName) || string.IsNullOrWhiteSpace(recipientLastName))
            return Result.Failure<Address>(Error.Validation("Address.InvalidName", "Recipient name is required."));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result.Failure<Address>(Error.Validation("Address.InvalidPhone", "Phone number is required."));

        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(street))
            return Result.Failure<Address>(Error.Validation("Address.InvalidLocation", "Country, city and street are required."));

        if (string.IsNullOrWhiteSpace(postalCode))
            return Result.Failure<Address>(Error.Validation("Address.InvalidPostalCode", "Postal code is required."));

        return Result.Success(new Address(
            recipientFirstName.Trim(),
            recipientLastName.Trim(),
            phoneNumber.Trim(),
            country.Trim(),
            city.Trim(),
            street.Trim(),
            postalCode.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RecipientFirstName;
        yield return RecipientLastName;
        yield return PhoneNumber;
        yield return Country;
        yield return City;
        yield return Street;
        yield return PostalCode;
    }
}
