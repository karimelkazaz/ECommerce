using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Queries;

public sealed record LoginQuery(string Email, string Password) : IRequest<Result<AuthResponse>>;

public sealed class LoginQueryHandler(IIdentityService identityService)
    : IRequestHandler<LoginQuery, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginQuery request, CancellationToken cancellationToken)
        => await identityService.LoginAsync(request.Email, request.Password, cancellationToken);
}

public sealed class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(q => q.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(q => q.Password)
            .NotEmpty();
    }
}