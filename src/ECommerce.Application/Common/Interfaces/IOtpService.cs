using ECommerce.Application.Auth;
using ECommerce.Domain.Common;

namespace ECommerce.Application.Common.Interfaces;

public interface IOtpService
{
    Task<Result> SendOtpAsync(string email, OtpPurpose purpose, CancellationToken ct);

    Task<Result> VerifyOtpAsync(string email, string code, OtpPurpose purpose, CancellationToken ct);

    Task<Result> ConfirmEmailAsync(string email, string code, CancellationToken ct);

    Task<Result> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct);
}