namespace ECommerce.Domain.Entities.Inventory
{
    public readonly record struct InventoryItemId(Guid Value)
    {
        public static InventoryItemId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
    }
}
