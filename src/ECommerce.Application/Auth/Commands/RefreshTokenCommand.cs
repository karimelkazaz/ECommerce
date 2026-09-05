using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;

public sealed class RefreshTokenCommandHandler(IIdentityService identityService)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        => await identityService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken)
            .NotEmpty();
    }
}