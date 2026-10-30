using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Queries;

public sealed record GetOrdersQuery(int Page = 1, int PageSize = 10, OrderStatus? Status = null)
    : IRequest<PagedResponse<OrderResponse>>;

public sealed class GetOrdersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetOrdersQuery, PagedResponse<OrderResponse>>
{
    public async Task<PagedResponse<OrderResponse>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        // GET /orders is authenticated, so a missing user is a defensive no-op rather than a 500.
        var userId = currentUser.UserId;
        if (userId is null)
            return new PagedResponse<OrderResponse>([], 1, 10, 0);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        IQueryable<Order> query = db.Orders
            .Where(o => o.UserId == userId.Value);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Order items are aggregate children: they load with the root through Include.
        var orders = await query
            .OrderByDescending(o => o.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.OrderItems)
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrderResponse>(
            orders.Select(OrderResponse.FromDomain).ToList(),
            page,
            pageSize,
            totalCount);
    }
}
