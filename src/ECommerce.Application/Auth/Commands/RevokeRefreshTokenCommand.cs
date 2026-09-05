using ECommerce.Domain.Common;
using ECommerce.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Commands;

public sealed record RevokeRefreshTokenCommand(string RefreshToken) : IRequest<Result>;

public sealed class RevokeRefreshTokenCommandHandler(IIdentityService identityService)
    : IRequestHandler<RevokeRefreshTokenCommand, Result>
{
    public async Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
        => await identityService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
}

public sealed class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    public RevokeRefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken)
            .NotEmpty();
    }
}