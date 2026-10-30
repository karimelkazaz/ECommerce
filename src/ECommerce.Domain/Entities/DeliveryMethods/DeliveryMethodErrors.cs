using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.DeliveryMethods;

public static class DeliveryMethodErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "DeliveryMethod.NotFound",
        "Delivery method was not found.");

    public static readonly Error InvalidId = Error.Validation(
        "DeliveryMethod.InvalidId",
        "Invalid delivery method ID.");

    public static readonly Error NameRequired = Error.Validation(
        "DeliveryMethod.NameRequired",
        "Name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "DeliveryMethod.NameTooLong",
        $"Name cannot exceed {DeliveryMethod.MaxNameLength} characters.");

    public static readonly Error DeliveryTimeRequired = Error.Validation(
        "DeliveryMethod.DeliveryTimeRequired",
        "Delivery time is required.");

    public static readonly Error DeliveryTimeTooLong = Error.Validation(
        "DeliveryMethod.DeliveryTimeTooLong",
        $"Delivery time cannot exceed {DeliveryMethod.MaxDeliveryTimeLength} characters.");

    public static readonly Error PriceRequired = Error.Validation(
        "DeliveryMethod.PriceRequired",
        "Price is required.");

    public static readonly Error InvalidPrice = Error.Validation(
        "DeliveryMethod.InvalidPrice",
        "Price cannot be negative.");

    public static readonly Error DescriptionTooLong = Error.Validation(
        "DeliveryMethod.DescriptionTooLong",
        $"Description cannot exceed {DeliveryMethod.MaxDescriptionLength} characters.");

    public static readonly Error NotAvailable = Error.Conflict(
        "DeliveryMethod.NotAvailable",
        "Delivery method is currently unavailable.");
}
