using ECommerce.Api.Common;
using ECommerce.Application.Auth;
using ECommerce.Application.Auth.Commands;
using ECommerce.Application.Auth.Queries;
using MediatR;

namespace ECommerce.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", Register).MapToApiVersion(1);
        group.MapPost("/login", Login).MapToApiVersion(1);
        group.MapGet("/me", GetCurrentUser).MapToApiVersion(1).RequireAuthorization();
        group.MapPut("/me", UpdateUserProfile).MapToApiVersion(1).RequireAuthorization();
        group.MapPost("/confirm-email", ConfirmEmail).MapToApiVersion(1);
        group.MapPost("/refresh", RefreshToken).MapToApiVersion(1);
        group.MapPost("/revoke", RevokeRefreshToken).MapToApiVersion(1);
        group.MapPost("/logout", Logout).MapToApiVersion(1);
        group.MapPost("/send-otp", SendOtp).MapToApiVersion(1);
        group.MapPost("/reset-password", ResetPassword).MapToApiVersion(1);

        return app;
    }

    private static async Task<IResult> Register(RegisterCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }

        var otpResult = await sender.Send(
            new SendOtpCommand(result.Value.Email, OtpPurpose.EmailVerification),
            ct);

        return otpResult.IsSuccess
            ? Results.Ok(result.Value)
            : otpResult.ToProblemDetails();
    }

    private static async Task<IResult> Login(LoginCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> GetCurrentUser(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> UpdateUserProfile(
        UpdateUserProfileRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateUserProfileCommand(request.FirstName, request.LastName),
            ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ConfirmEmail(ConfirmEmailCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> RefreshToken(RefreshTokenCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> RevokeRefreshToken(RevokeRefreshTokenCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Logout(LogoutCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> SendOtp(SendOtpCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ResetPassword(ResetPasswordCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToProblemDetails();
    }
}

public sealed record UpdateUserProfileRequest(
    string FirstName,
    string LastName);