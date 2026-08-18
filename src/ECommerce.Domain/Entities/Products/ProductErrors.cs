using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Products
{
    public static class ProductErrors
    {
        public static readonly Error NotFound = Error.NotFound("Product.NotFound", "Product not found.");
        public static readonly Error InvalidId = Error.Validation("Product.InvalidId", "Invalid product ID.");
        public static readonly Error NameRequired = Error.Validation("Product.NameRequired", "Name is required.");
        public static readonly Error NameTooLong =
        Error.Validation(
        "Product.NameTooLong",
        $"Product name cannot exceed {Product.MaxNameLength} characters.");
        public static readonly Error DescriptionRequired = Error.Validation("Product.DescriptionRequired", "Description is required.");

        public static readonly Error DescriptionTooLong =
            Error.Validation(
                "Product.DescriptionTooLong",
                $"Product description cannot exceed {Product.MaxDescriptionLength} characters.");
        public static readonly Error PictureUrlRequired = Error.Validation("Product.PictureUrlRequired", "Picture URL is required.");
        public static readonly Error PictureUrlTooLong =
    Error.Validation(
        "Product.PictureUrlTooLong",
        $"Product picture URL cannot exceed {Product.MaxPictureUrlLength} characters.");
        public static readonly Error InvalidPrice = Error.Validation("Product.InvalidPrice", "Invalid price.");

        public static readonly Error InvalidBrand =
    Error.Validation(
        "Product.InvalidBrand",
        "Product brand is required.");

        public static readonly Error InvalidType =
            Error.Validation(
                "Product.InvalidType",
                "Product type is required.");

        public static readonly Error DuplicateName =
            Error.Conflict(
                "Product.DuplicateName",
                "Product name already exists.");
    }
}