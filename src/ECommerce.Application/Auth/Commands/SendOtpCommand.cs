using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Commands;

public sealed record SendOtpCommand(string Email, OtpPurpose Purpose) : IRequest<Result>;

public sealed class SendOtpCommandHandler(IOtpService otpService)
    : IRequestHandler<SendOtpCommand, Result>
{
    public async Task<Result> Handle(SendOtpCommand request, CancellationToken cancellationToken)
        => await otpService.SendOtpAsync(request.Email, request.Purpose, cancellationToken);
}

public sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(c => c.Purpose)
            .IsInEnum();
    }
}