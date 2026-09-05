using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Commands;

public sealed record VerifyOtpCommand(string Email, string Code, OtpPurpose Purpose) : IRequest<Result>;

public sealed class VerifyOtpCommandHandler(IOtpService otpService)
    : IRequestHandler<VerifyOtpCommand, Result>
{
    public async Task<Result> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        => await otpService.VerifyOtpAsync(request.Email, request.Code, request.Purpose, cancellationToken);
}

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(c => c.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$");

        RuleFor(c => c.Purpose)
            .IsInEnum();
    }
}