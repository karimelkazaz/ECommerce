using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Auth.Commands;

public sealed record UpdateUserProfileCommand(string FirstName, string LastName)
    : IRequest<Result<UserProfileResponse>>;

public sealed class UpdateUserProfileCommandHandler(
    IIdentityService identityService,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateUserProfileCommand, Result<UserProfileResponse>>
{
    public async Task<Result<UserProfileResponse>> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure<UserProfileResponse>(AuthErrors.UserNotFound);

        return await identityService.UpdateUserProfileAsync(
            userId.Value,
            request.FirstName,
            request.LastName,
            cancellationToken);
    }
}

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
