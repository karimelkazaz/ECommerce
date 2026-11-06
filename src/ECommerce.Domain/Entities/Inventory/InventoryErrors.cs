using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Inventory
{
    public static class InventoryErrors
    {
        public static readonly Error NotFound = Error.NotFound("Inventory.NotFound", "Inventory record not found.");
        public static readonly Error InvalidId = Error.Validation("Inventory.InvalidId", "Invalid product ID.");
        public static readonly Error InvalidQuantity = Error.Validation("Inventory.InvalidQuantity", "Invalid quantity.");
        public static readonly Error InsufficientStock = Error.Conflict("Inventory.InsufficientStock", "Insufficient stock for the requested quantity.");
        public static readonly Error Duplicate = Error.Conflict("Inventory.Duplicate", "An inventory record already exists for this product.");
        public static readonly Error ProductNotTracked = Error.Conflict("Inventory.ProductNotTracked", "Product has no inventory record and cannot be purchased.");
        public static readonly Error LockUnavailable = Error.Failure("Inventory.LockUnavailable", "Stock reservation timed out. Please try again.");
        public static readonly Error ConcurrencyConflict = Error.Conflict("Inventory.ConcurrencyConflict", "Stock changed while processing. Please retry.");
    }
}
