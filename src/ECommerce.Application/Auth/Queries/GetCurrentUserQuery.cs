using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using MediatR;

namespace ECommerce.Application.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<Result<UserProfileResponse>>;

public sealed class GetCurrentUserQueryHandler(
    IIdentityService identityService,
    ICurrentUserService currentUser)
    : IRequestHandler<GetCurrentUserQuery, Result<UserProfileResponse>>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        // GET /me is authenticated, so a missing user id is a defensive no-op rather than a 500.
        var userId = currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure<UserProfileResponse>(AuthErrors.UserNotFound);

        return await identityService.GetUserProfileAsync(userId.Value, cancellationToken);
    }
}
