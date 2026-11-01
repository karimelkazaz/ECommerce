namespace ECommerce.Application.Auth;

public sealed record UserProfileResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<string> Roles);
