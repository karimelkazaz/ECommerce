using System.Text;
using Asp.Versioning.Builder;
using ECommerce.Api.Common;
using ECommerce.Application.Common;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.Commands;
using ECommerce.Application.Orders.Queries;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using MediatR;

namespace ECommerce.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");

        group.MapGet("/", GetOrders).MapToApiVersion(1).RequireAuthorization();
        group.MapGet("/{id:guid}", GetOrderById).WithName("GetOrderById").MapToApiVersion(1).RequireAuthorization();
        group.MapPost("/checkout", Checkout).MapToApiVersion(1).RequireAuthorization();
        group.MapPost("/{id:guid}/cancel", CancelOwnOrder).MapToApiVersion(1).RequireAuthorization();
        group.MapPatch("/{id:guid}/ship", ShipOrder).MapToApiVersion(1).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{id:guid}/deliver", DeliverOrder).MapToApiVersion(1).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{id:guid}/cancel", AdminCancelOrder).MapToApiVersion(1).RequireAuthorization(Roles.Admin);

        // Stripe requires an anonymous endpoint: it authenticates with the Stripe-Signature header instead.
        app.MapPost("/webhooks/stripe", HandleStripeWebhook).MapToApiVersion(1);

        return app;
    }

    private static async Task<IResult> GetOrders(
        ISender sender,
        int page = 1,
        int pageSize = 10,
        OrderStatus? status = null,
        CancellationToken ct = default)
        => Results.Ok(await sender.Send(new GetOrdersQuery(page, pageSize, status), ct));

    private static async Task<IResult> GetOrderById(Guid id, ISender sender, CancellationToken ct)
    {
        var order = await sender.Send(new GetOrderByIdQuery(id), ct);

        return order is not null
            ? Results.Ok(order)
            : OrderErrors.NotFound.ToProblemDetails();
    }

    private static async Task<IResult> Checkout(CheckoutCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetOrderById", new { id = result.Value.OrderId, version = "1" }, result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> CancelOwnOrder(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new CancelOwnOrderCommand(id), ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ShipOrder(Guid id, ISender sender, CancellationToken ct)
        => await SendTransition(new ShipOrderCommand(id), sender, ct);

    private static async Task<IResult> DeliverOrder(Guid id, ISender sender, CancellationToken ct)
        => await SendTransition(new DeliverOrderCommand(id), sender, ct);

    private static async Task<IResult> AdminCancelOrder(Guid id, ISender sender, CancellationToken ct)
        => await SendTransition(new CancelOrderCommand(id, "Cancelled by admin"), sender, ct);

    private static async Task<IResult> SendTransition(IRequest<Result> command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> HandleStripeWebhook(HttpRequest request, ISender sender, CancellationToken ct)
    {
        string json;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8))
        {
            json = await reader.ReadToEndAsync(ct);
        }

        var signatureHeader = request.Headers["Stripe-Signature"].ToString();

        var result = await sender.Send(new HandleStripeWebhookCommand(json, signatureHeader), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }
}
