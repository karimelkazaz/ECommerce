namespace ECommerce.Domain.Entities.ProductsBrand
{
    public readonly record struct ProductBrandId(Guid Value)
    {
        public static ProductBrandId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
    }
}