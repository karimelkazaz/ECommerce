using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.Entities.UserAddresses;

namespace ECommerce.Application.Common.Interfaces;

public interface IApplicationDbContext : IUnitOfWork
{
    IQueryable<Product> Products { get; }
    IQueryable<ProductBrand> Brands { get; }
    IQueryable<ProductType> Types { get; }
    IQueryable<DeliveryMethod> DeliveryMethods { get; }
    IQueryable<UserAddress> UserAddresses { get; }
}