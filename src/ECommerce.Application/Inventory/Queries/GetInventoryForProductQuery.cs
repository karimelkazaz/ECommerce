using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Inventory;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Inventory.Queries;

public sealed record GetInventoryForProductQuery(Guid ProductId) : IRequest<InventoryItemResponse?>;

public sealed class GetInventoryForProductQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInventoryForProductQuery, InventoryItemResponse?>
{
    public async Task<InventoryItemResponse?> Handle(GetInventoryForProductQuery request, CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        return await db.InventoryItems
            .Where(i => i.ProductId == productId)
            .Select(InventoryItemResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
