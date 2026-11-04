using ECommerce.Api.Common;
using ECommerce.Application.Inventory;
using ECommerce.Application.Inventory.Commands;
using ECommerce.Application.Inventory.Queries;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using MediatR;

namespace ECommerce.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory").WithTags("Inventory");

        group.MapGet("/{productId:guid}", GetInventoryItem).WithName("GetInventoryItem").MapToApiVersion(1).RequireAuthorization();
        group.MapPost("/", CreateInventoryItem).MapToApiVersion(1).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{productId:guid}/adjust", AdjustInventoryItem).MapToApiVersion(1).RequireAuthorization(Roles.Admin);

        return app;
    }

    private static async Task<IResult> GetInventoryItem(Guid productId, ISender sender, CancellationToken ct)
    {
        var response = await sender.Send(new GetInventoryForProductQuery(productId), ct);

        return response is not null
            ? Results.Ok(response)
            : InventoryErrors.NotFound.ToProblemDetails();
    }

    private static async Task<IResult> CreateInventoryItem(CreateInventoryItemCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetInventoryItem", new { productId = result.Value.ProductId, version = "1" }, result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> AdjustInventoryItem(
        Guid productId,
        AdjustInventoryItemCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command with { ProductId = productId }, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }
}
