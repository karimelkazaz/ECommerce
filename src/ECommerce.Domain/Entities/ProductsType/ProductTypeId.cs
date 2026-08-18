namespace ECommerce.Domain.Entities.ProductsType
{
    public readonly record struct ProductTypeId(Guid Value)
    {
        public static ProductTypeId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
    }
}