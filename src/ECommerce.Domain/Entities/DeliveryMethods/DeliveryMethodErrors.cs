using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.DeliveryMethods;

public static class DeliveryMethodErrors
{
    public static readonly Error InvalidId = Error.Validation(
        "DeliveryMethod.InvalidId", 
        "Invalid delivery method ID.");

    public static readonly Error NameRequired = Error.Validation(
        "DeliveryMethod.NameRequired",
        "Name is required.");

    public static readonly Error DeliveryTimeRequired = Error.Validation(
        "DeliveryMethod.DeliveryTimeRequired",
        "Delivery time is required.");

    public static readonly Error PriceRequired = Error.Validation(
        "DeliveryMethod.PriceRequired", 
        "Price is required.");
}