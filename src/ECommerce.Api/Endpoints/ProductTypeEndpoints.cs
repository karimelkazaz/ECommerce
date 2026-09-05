using ECommerce.Api.Common;
using ECommerce.Application.ProductsType.Commands;
using ECommerce.Application.ProductsType.Queries;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.ProductsType;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Endpoints;

public static class ProductTypeEndpoints
{
    public static IEndpointRouteBuilder MapProductTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/types").WithTags("Types");

        group.MapGet("/", GetTypes).RequireAuthorization();
        group.MapGet("/{id:guid}", GetTypeById).WithName("GetTypeById").RequireAuthorization();
        group.MapPost("/", CreateType).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{id:guid}/name", RenameType).RequireAuthorization(Roles.Admin);
        group.MapDelete("/{id:guid}", DeleteType).RequireAuthorization(Roles.Admin);

        return app;
    }

    private static async Task<IResult> GetTypes(ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetTypesQuery(), ct));

    private static async Task<IResult> GetTypeById(Guid id, ISender sender, CancellationToken ct)
    {
        var type = await sender.Send(new GetTypeByIdQuery(id), ct);

        return type is not null
            ? Results.Ok(type)
            : ProductTypeErrors.NotFound.ToProblemDetails();
    }

    private static async Task<IResult> CreateType(CreateTypeCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetTypeById", new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> RenameType(
        Guid id,
        RenameTypeCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command with { Id = id }, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> DeleteType(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteTypeCommand(id), ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }
}
