using ECommerce.Api.Common;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Commands;
using ECommerce.Application.Products.Queries;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetProducts).RequireAuthorization();
        group.MapGet("/{id:guid}", GetProductById).WithName("GetProductById").RequireAuthorization();
        group.MapPost("/", CreateProduct).RequireAuthorization(Roles.Admin);
        group.MapPatch("/{id:guid}/price", ChangePrice).RequireAuthorization(Roles.Admin);
        group.MapDelete("/{id:guid}", DeleteProduct).RequireAuthorization(Roles.Admin);

        return app;
    }

    private static async Task<IResult> GetProducts(
        ISender sender,
        CancellationToken ct,
        [FromQuery] string? search = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] Guid? typeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ProductSortField sortBy = ProductSortField.Name,
        [FromQuery] bool sortDescending = false)
    {
        var response = await sender.Send(
            new GetProductsQuery(search, brandId, typeId, page, pageSize, sortBy, sortDescending), ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetProductById(Guid id, ISender sender, CancellationToken ct)
    {
        var product = await sender.Send(new GetProductByIdQuery(id), ct);

        return product is not null
            ? Results.Ok(product)
            : ProductErrors.NotFound.ToProblemDetails();
    }

    private static async Task<IResult> CreateProduct(CreateProductCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetProductById", new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ChangePrice(
        Guid id,
        ChangeProductPriceCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command with { Id = id }, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> DeleteProduct(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteProductCommand(id), ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }
}
