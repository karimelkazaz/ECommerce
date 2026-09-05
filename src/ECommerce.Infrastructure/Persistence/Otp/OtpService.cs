using System.Security.Cryptography;
using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Persistence.Otp;

public sealed class OtpService(
    AppIdentityDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<OtpOptions> options,
    ILogger<OtpService> logger) : IOtpService
{
    private readonly OtpOptions _otp = options.Value;

    public async Task<Result> SendOtpAsync(string email, OtpPurpose purpose, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        email = NormalizeEmail(email);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            logger.LogWarning("OTP requested for unknown e-mail {Email}.", email);
            return Result.Success();
        }

        var now = DateTimeOffset.UtcNow;

        var activeCodes = await dbContext.OtpCodes
            .Where(o => o.Email == email && o.Purpose == purpose.ToString() && o.InvalidatedOnUtc == null)
            .ToListAsync(ct);

        foreach (var active in activeCodes)
        {
            active.InvalidatedOnUtc = now;
        }

        var code = GenerateCode(_otp.CodeLength);
        dbContext.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            Purpose = purpose.ToString(),
            CodeHash = HashCode(code),
            CreatedOnUtc = now,
            ExpiresOnUtc = now.AddMinutes(_otp.ExpirationMinutes),
        });

        await dbContext.SaveChangesAsync(ct);

        var subject = "Your ECommerce verification code";
        var body = $"""
            <p>Hello {user.FirstName},</p>
            <p>Your one-time verification code is:</p>
            <p style="font-size:24px;font-weight:bold;letter-spacing:4px;">{code}</p>
            <p>This code expires in {_otp.ExpirationMinutes} minutes. Do not share it with anyone.</p>
            """;

        await emailSender.SendAsync(email, subject, body, ct);

        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(string email, string code, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        email = NormalizeEmail(email);

        var (verifyResult, otp) = await VerifyCoreAsync(email, code, OtpPurpose.EmailVerification, ct);
        if (verifyResult.IsFailure || otp is null)
        {
            return verifyResult;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidOtp);
        }

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            otp.InvalidatedOnUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }

        user.EmailConfirmed = true;
        var confirmResult = await userManager.UpdateAsync(user);
        if (!confirmResult.Succeeded)
        {
            return Result.Failure(AuthErrors.RegistrationFailed);
        }

        otp.InvalidatedOnUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> VerifyOtpAsync(string email, string code, OtpPurpose purpose, CancellationToken ct)
    {
        var (result, _) = await VerifyCoreAsync(email, code, purpose, ct);
        return result;
    }

    public async Task<Result> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        email = NormalizeEmail(email);

        var (verifyResult, otp) = await VerifyCoreAsync(email, code, OtpPurpose.PasswordReset, ct);
        if (verifyResult.IsFailure || otp is null)
        {
            return verifyResult;
        }

        otp.InvalidatedOnUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidOtp);
        }

        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            return Result.Failure(AuthErrors.PasswordChangeFailed);
        }

        var addResult = await userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
        {
            return Result.Failure(AuthErrors.PasswordChangeFailed);
        }

        return Result.Success();
    }

    private async Task<(Result Result, OtpCode? Code)> VerifyCoreAsync(
        string email,
        string code,
        OtpPurpose purpose,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        email = NormalizeEmail(email);

        var now = DateTimeOffset.UtcNow;

        var otp = await dbContext.OtpCodes
            .Where(o => o.Email == email
                        && o.Purpose == purpose.ToString()
                        && o.InvalidatedOnUtc == null)
            .OrderByDescending(o => o.CreatedOnUtc)
            .FirstOrDefaultAsync(ct);

        if (otp is null)
        {
            return (Result.Failure(AuthErrors.OtpNotRequested), null);
        }

        if (otp.ExpiresOnUtc < now)
        {
            otp.InvalidatedOnUtc = now;
            await dbContext.SaveChangesAsync(ct);
            return (Result.Failure(AuthErrors.ExpiredOtp), null);
        }

        var codeHash = HashCode(code);
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(codeHash),
                Convert.FromHexString(otp.CodeHash)))
        {
            otp.FailedAttempts++;

            if (otp.FailedAttempts >= _otp.MaxFailedAttempts)
            {
                otp.InvalidatedOnUtc = now;
                await dbContext.SaveChangesAsync(ct);
                return (Result.Failure(AuthErrors.TooManyOtpAttempts), null);
            }

            await dbContext.SaveChangesAsync(ct);
            return (Result.Failure(AuthErrors.InvalidOtp), null);
        }

        otp.VerifiedOnUtc ??= now;
        await dbContext.SaveChangesAsync(ct);

        return (Result.Success(), otp);
    }

    private static string GenerateCode(int length)
        => RandomNumberGenerator
            .GetInt32(0, (int)Math.Pow(10, length))
            .ToString($"D{length}");

    private static string HashCode(string code)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
}