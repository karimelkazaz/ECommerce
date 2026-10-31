using ECommerce.Api.Common;
using ECommerce.Application.UserAddresses.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Endpoints;

public static class UserAddressEndpoints
{
    public static IEndpointRouteBuilder MapUserAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user-addresses")
            .WithTags("User Addresses")
            .RequireAuthorization();

        group.MapPost("/", AddUserAddress).MapToApiVersion(1);
        group.MapPut("/{id:guid}", UpdateUserAddress).MapToApiVersion(1);

        return app;
    }

    private static async Task<IResult> AddUserAddress(
        [FromBody] UserAddressRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddUserAddressCommand(
            request.Label,
            request.IsDefault,
            request.RecipientFirstName,
            request.RecipientLastName,
            request.PhoneNumber,
            request.Country,
            request.City,
            request.Street,
            request.PostalCode), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> UpdateUserAddress(
        Guid id,
        [FromBody] UserAddressRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateUserAddressCommand(
            id,
            request.Label,
            request.IsDefault,
            request.RecipientFirstName,
            request.RecipientLastName,
            request.PhoneNumber,
            request.Country,
            request.City,
            request.Street,
            request.PostalCode), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UserAddressRequest(
    string Label,
    bool IsDefault,
    string RecipientFirstName,
    string RecipientLastName,
    string PhoneNumber,
    string Country,
    string City,
    string Street,
    string PostalCode);