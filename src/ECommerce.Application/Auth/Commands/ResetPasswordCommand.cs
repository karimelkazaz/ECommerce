using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Commands;

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : IRequest<Result>;

public sealed class ResetPasswordCommandHandler(IOtpService otpService)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        => await otpService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword, cancellationToken);
}

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(c => c.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$");

        RuleFor(c => c.NewPassword)
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