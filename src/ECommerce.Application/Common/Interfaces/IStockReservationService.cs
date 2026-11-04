using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;

namespace ECommerce.Application.Common.Interfaces;

public sealed record StockReservationLine(ProductId ProductId, string ProductName, int Quantity);

public interface IStockReservationService
{
    /// <summary>
    /// Lock-protected, all-or-nothing stock reservation for an order. The
    /// reservation commits together with every pending change on the ambient
    /// unit of work (the newly created checkout order included), so an order
    /// and its stock reservation can never exist independently.
    /// </summary>
    Task<Result> ReserveForOrderAsync(
        OrderId orderId,
        IReadOnlyList<StockReservationLine> lines,
        CancellationToken cancellationToken = default);
}
