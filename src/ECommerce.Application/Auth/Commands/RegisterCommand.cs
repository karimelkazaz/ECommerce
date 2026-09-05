using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Commands;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<Result<EmailSentResponse>>;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IOtpService otpService) : IRequestHandler<RegisterCommand, Result<EmailSentResponse>>
{
    public const string RegisteredMessage =
        "Registration successful. A verification code was sent to your email. Confirm your email before logging in.";

    public const string UnconfirmedResendMessage =
        "This email is registered but not confirmed. A new verification code was sent to your email.";

    public async Task<Result<EmailSentResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        if (await identityService.UserExistsAsync(email, cancellationToken))
        {
            if (await identityService.IsEmailConfirmedAsync(email, cancellationToken))
            {
                return Result.Failure<EmailSentResponse>(AuthErrors.EmailAlreadyRegistered);
            }

            return await SendVerificationAsync(
                email,
                verificationCodeResent: true,
                UnconfirmedResendMessage,
                cancellationToken);
        }

        var createResult = await identityService.CreateUserAsync(
            request.FirstName,
            request.LastName,
            email,
            request.Password,
            cancellationToken);

        if (createResult.IsFailure)
        {
            return Result.Failure<EmailSentResponse>(createResult.Error);
        }

        return await SendVerificationAsync(
            email,
            verificationCodeResent: false,
            RegisteredMessage,
            cancellationToken);
    }

    private async Task<Result<EmailSentResponse>> SendVerificationAsync(
        string email,
        bool verificationCodeResent,
        string message,
        CancellationToken cancellationToken)
    {
        var sendResult = await otpService.SendOtpAsync(email, OtpPurpose.EmailVerification, cancellationToken);

        if (sendResult.IsFailure)
        {
            return Result.Failure<EmailSentResponse>(sendResult.Error);
        }

        return new EmailSentResponse(email, verificationCodeResent, message);
    }
}

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(c => c.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Must(password => password.Any(char.IsLower))
                .WithMessage("Password must contain at least one lowercase letter.")
            .Must(password => password.Any(char.IsUpper))
                .WithMessage("Password must contain at least one uppercase letter.")
            .Must(password => password.Any(char.IsDigit))
                .WithMessage("Password must contain at least one digit.");
    }
}