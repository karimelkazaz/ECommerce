using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.ProductsType
{
    public static class ProductTypeErrors
    {
        public static readonly Error NotFound =
            Error.NotFound(
                "ProductType.NotFound",
                "Type was not found.");

        public static readonly Error InvalidId =
            Error.Validation(
                "ProductType.InvalidId",
                "Type id is required.");

        public static readonly Error InvalidName =
            Error.Validation(
                "ProductType.InvalidName",
                "Type name is required.");

        public static readonly Error NameTooLong =
            Error.Validation(
                "ProductType.NameTooLong",
                $"Type name cannot exceed {ProductType.MaxNameLength} characters.");
    }
}