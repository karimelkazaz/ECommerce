using ECommerce.Api.Common;
using ECommerce.Application.Baskets.Commands;
using ECommerce.Application.Baskets.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Endpoints;

public static class BasketEndpoints
{
    public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/basket").WithTags("Basket");

        group.MapGet("/", GetBasket).MapToApiVersion(1).RequireAuthorization();
        group.MapPost("/items", AddItem).MapToApiVersion(1).RequireAuthorization();
        group.MapPut("/items/{productId:guid}", UpdateQuantity).MapToApiVersion(1).RequireAuthorization();
        group.MapDelete("/items/{productId:guid}", RemoveItem).MapToApiVersion(1).RequireAuthorization();
        group.MapDelete("/", ClearBasket).MapToApiVersion(1).RequireAuthorization();
        group.MapPost("/merge", MergeBasket).MapToApiVersion(1).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetBasket(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetBasketQuery(), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> AddItem(
        [FromBody] AddBasketItemRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new AddItemToBasketCommand(request.ProductId, request.Quantity), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> UpdateQuantity(
        Guid productId,
        [FromBody] UpdateBasketItemQuantityRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateBasketItemQuantityCommand(productId, request.Quantity), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> RemoveItem(
        Guid productId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new RemoveBasketItemCommand(productId), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ClearBasket(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ClearBasketCommand(), ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> MergeBasket(
        [FromBody] MergeBasketRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new MergeBasketCommand(request.SourceUserId), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }
}

public sealed record AddBasketItemRequest(Guid ProductId, int Quantity = 1);
public sealed record UpdateBasketItemQuantityRequest(int Quantity);
public sealed record MergeBasketRequest(Guid SourceUserId);

