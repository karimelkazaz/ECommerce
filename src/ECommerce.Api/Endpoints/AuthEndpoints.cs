using ECommerce.Api.Common;
using ECommerce.Application.Auth.Commands;
using ECommerce.Application.Auth.Queries;
using MediatR;

namespace ECommerce.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapPost("/confirm-email", ConfirmEmail);
        group.MapPost("/refresh", RefreshToken);
        group.MapPost("/revoke", RevokeRefreshToken);
        group.MapPost("/send-otp", SendOtp);
        group.MapPost("/verify-otp", VerifyOtp);
        group.MapPost("/reset-password", ResetPassword);

        return app;
    }

    private static async Task<IResult> Register(RegisterCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Login(LoginQuery query, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);

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

    private static async Task<IResult> SendOtp(SendOtpCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> VerifyOtp(VerifyOtpCommand command, ISender sender, CancellationToken ct)
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