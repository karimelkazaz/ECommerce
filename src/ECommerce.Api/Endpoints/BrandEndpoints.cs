using ECommerce.Api.Common;
using ECommerce.Application.ProductsBrand.Commands;
using ECommerce.Application.ProductsBrand.Queries;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.ProductsBrand;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Endpoints;

public static class BrandEndpoints
{
    public static IEndpointRouteBuilder MapBrandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/brands").WithTags("Brands");

        group.MapGet("/", GetBrands).RequireAuthorization();
        group.MapGet("/{id:guid}", GetBrandById).WithName("GetBrandById").RequireAuthorization();
        group.MapPost("/", CreateBrand).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{id:guid}/name", RenameBrand).RequireAuthorization(Roles.Admin);
        group.MapDelete("/{id:guid}", DeleteBrand).RequireAuthorization(Roles.Admin);

        return app;
    }

    private static async Task<IResult> GetBrands(ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetBrandsQuery(), ct));

    private static async Task<IResult> GetBrandById(Guid id, ISender sender, CancellationToken ct)
    {
        var brand = await sender.Send(new GetBrandByIdQuery(id), ct);

        return brand is not null
            ? Results.Ok(brand)
            : ProductBrandErrors.NotFound.ToProblemDetails();
    }

    private static async Task<IResult> CreateBrand(CreateBrandCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetBrandById", new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> RenameBrand(
        Guid id,
        RenameBrandCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command with { Id = id }, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> DeleteBrand(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteBrandCommand(id), ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }
}
