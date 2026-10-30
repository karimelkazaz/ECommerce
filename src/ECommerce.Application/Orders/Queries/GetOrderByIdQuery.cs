using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderResponse?>;

public sealed class GetOrderByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetOrderByIdQuery, OrderResponse?>
{
    public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return null;

        var order = await db.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.Id == new OrderId(request.Id) && o.UserId == userId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return order is null ? null : OrderResponse.FromDomain(order);
    }
}
