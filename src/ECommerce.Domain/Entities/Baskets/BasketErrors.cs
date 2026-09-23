using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Baskets;

public static class BasketErrors
{
    public static readonly Error NotFound = Error.NotFound("Basket.NotFound", "Basket not found.");
    public static readonly Error InvalidUserId = Error.Validation("Basket.InvalidUserId", "User ID is required.");
    public static readonly Error CurrencyMismatch = Error.Validation("Basket.CurrencyMismatch", "Currencies must match across all items.");

    public static readonly Error InvalidProductId =
        Error.Validation("Basket.InvalidProductId", "Product id is required.");

    public static readonly Error ProductNameRequired =
        Error.Validation("Basket.ProductNameRequired", "Product name is required.");

    public static readonly Error PictureUrlRequired =
        Error.Validation("Basket.PictureUrlRequired", "Picture URL is required.");

    public static readonly Error PriceRequired =
        Error.Validation("Basket.PriceRequired", "Price is required.");

    public static readonly Error InvalidQuantity =
        Error.Validation("Basket.InvalidQuantity", "Quantity must be at least 1.");

    public static readonly Error ItemNotFound =
        Error.NotFound("Basket.ItemNotFound", "Basket item not found.");

    public static readonly Error MergeSourceRequired =
        Error.Validation("Basket.MergeSourceRequired", "Merge source basket is required.");

    public static readonly Error CannotMergeSelf =
        Error.Validation("Basket.CannotMergeSelf", "Cannot merge a basket into itself.");
}
