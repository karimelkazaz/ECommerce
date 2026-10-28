using Asp.Versioning.Builder;
using ECommerce.Api.Common;
using ECommerce.Application.DeliveryMethods.Commands;
using ECommerce.Application.DeliveryMethods.Queries;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using MediatR;

namespace ECommerce.Api.Endpoints;

public static class DeliveryMethodEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryMethodEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/delivery-methods").WithTags("DeliveryMethods");

        group.MapGet("/", GetDeliveryMethods).MapToApiVersion(1).RequireAuthorization();
        group.MapGet("/{id:guid}", GetDeliveryMethodById).WithName("GetDeliveryMethodById").MapToApiVersion(1).RequireAuthorization();
        group.MapPost("/", CreateDeliveryMethod).MapToApiVersion(1).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{id:guid}/name", RenameDeliveryMethod).MapToApiVersion(1).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{id:guid}/delivery-time", ChangeDeliveryTime).MapToApiVersion(1).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{id:guid}/price", ChangePrice).MapToApiVersion(1).RequireAuthorization(Roles.Admin);
        group.MapDelete("/{id:guid}", DeleteDeliveryMethod).MapToApiVersion(1).RequireAuthorization(Roles.Admin);

        return app;
    }

    private static async Task<IResult> GetDeliveryMethods(ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetDeliveryMethodsQuery(), ct));

    private static async Task<IResult> GetDeliveryMethodById(Guid id, ISender sender, CancellationToken ct)
    {
        var deliveryMethod = await sender.Send(new GetDeliveryMethodByIdQuery(id), ct);

        return deliveryMethod is not null
            ? Results.Ok(deliveryMethod)
            : DeliveryMethodErrors.NotFound.ToProblemDetails();
    }

    private static async Task<IResult> CreateDeliveryMethod(CreateDeliveryMethodCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetDeliveryMethodById", new { id = result.Value.Id, version = "1" }, result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> RenameDeliveryMethod(
        Guid id,
        RenameDeliveryMethodCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command with { Id = id }, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ChangeDeliveryTime(
        Guid id,
        ChangeDeliveryMethodDeliveryTimeCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command with { Id = id }, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ChangePrice(
        Guid id,
        ChangeDeliveryMethodPriceCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command with { Id = id }, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> DeleteDeliveryMethod(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteDeliveryMethodCommand(id), ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }
}
