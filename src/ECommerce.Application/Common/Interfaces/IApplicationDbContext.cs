using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;

namespace ECommerce.Application.Common.Interfaces;

public interface IApplicationDbContext : IUnitOfWork
{
    IQueryable<Product> Products { get; }
    IQueryable<ProductBrand> Brands { get; }
    IQueryable<ProductType> Types { get; }
}