using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Commands;

public sealed record ConfirmEmailCommand(string Email, string Code) : IRequest<Result>;

public sealed class ConfirmEmailCommandHandler(IOtpService otpService)
    : IRequestHandler<ConfirmEmailCommand, Result>
{
    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        => await otpService.ConfirmEmailAsync(request.Email, request.Code, cancellationToken);
}

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(c => c.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$");
    }
}