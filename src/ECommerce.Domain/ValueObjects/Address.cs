using ECommerce.Domain.Common;

namespace ECommerce.Domain.ValueObjects
{
    public sealed class Address : ValueObject
    {

        public Guid UserId { get; private set; }
        public string Label { get; private set; } = null!;
        public string RecipientFirstName { get; private set; } = null!;
        public string RecipientLastName { get; private set; } = null!;
        public string PhoneNumber { get; private set; } = null!;
        public string Country { get; private set; } = null!;
        public string City { get; private set; } = null!;
        public string Street { get; private set; } = null!;
        public string PostalCode { get; private set; } = null!;
        public bool IsDefaultShipping { get; private set; }
        public bool IsDefaultBilling { get; private set; }

        private Address() {}
        private Address(
            Guid userId, 
            string label, 
            string recipientFirstName, 
            string recipientLastName, 
            string phoneNumber, 
            string country, 
            string city, 
            string street, 
            string postalCode, 
            bool isDefaultShipping, 
            bool isDefaultBilling)
        {
            UserId = userId;
            Label = label;
            RecipientFirstName = recipientFirstName;
            RecipientLastName = recipientLastName;
            PhoneNumber = phoneNumber;
            Country = country;
            City = city;
            Street = street;
            PostalCode = postalCode;
            IsDefaultShipping = isDefaultShipping;
            IsDefaultBilling = isDefaultBilling;
        }


   public static Result<Address> Create(
        Guid userId,
        string label,
        string recipientFirstName,
        string recipientLastName,
        string phoneNumber,
        string country,
        string city,
        string street,
        string postalCode,
        bool isDefaultShipping = false,
        bool isDefaultBilling = false)
    {

        if (userId == Guid.Empty)
            return Result.Failure<Address>(Error.Validation("Address.InvalidUserId", "User id is required."));

        if (string.IsNullOrWhiteSpace(label))
            return Result.Failure<Address>(Error.Validation("Address.InvalidLabel", "Label is required."));

        if (string.IsNullOrWhiteSpace(recipientFirstName) || string.IsNullOrWhiteSpace(recipientLastName))
            return Result.Failure<Address>(Error.Validation("Address.InvalidName", "Recipient name is required."));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result.Failure<Address>(Error.Validation("Address.InvalidPhone", "Phone number is required."));

        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(street))
            return Result.Failure<Address>(Error.Validation("Address.InvalidLocation", "Country, city and street are required."));

        if (string.IsNullOrWhiteSpace(postalCode))
            return Result.Failure<Address>(Error.Validation("Address.InvalidPostalCode", "Postal code is required."));

        return Result.Success(new Address
        {
            UserId = userId,
            Label = label.Trim(),
            RecipientFirstName = recipientFirstName.Trim(),
            RecipientLastName = recipientLastName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Country = country.Trim(),
            City = city.Trim(),
            Street = street.Trim(),
            PostalCode = postalCode.Trim(),
            IsDefaultShipping = isDefaultShipping,
            IsDefaultBilling = isDefaultBilling,
        });
    }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return UserId;
            yield return Label;
            yield return RecipientFirstName;
            yield return RecipientLastName;
            yield return PhoneNumber;
            yield return Country;
            yield return City;
            yield return Street;
            yield return PostalCode;
            yield return IsDefaultShipping;
            yield return IsDefaultBilling;
        }
    }
}