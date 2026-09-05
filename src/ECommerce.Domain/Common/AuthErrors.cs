namespace ECommerce.Domain.Common;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid email or password.", ErrorType.Unauthorized);

    public static readonly Error AccountLocked =
        new("Auth.AccountLocked", "Account is temporarily locked due to repeated failed login attempts.", ErrorType.Forbidden);

    public static readonly Error EmailAlreadyRegistered =
        new("Auth.EmailAlreadyRegistered", "A user with this email is already registered.", ErrorType.Conflict);

    public static readonly Error RegistrationFailed =
        new("Auth.RegistrationFailed", "Could not register the account. Please try again.", ErrorType.Problem);

    public static readonly Error RoleAssignmentFailed =
        new("Auth.RoleAssignmentFailed", "Could not assign the default role to the new account.", ErrorType.Problem);

    public static readonly Error InvalidRefreshToken =
        new("Auth.InvalidRefreshToken", "The refresh token is invalid, revoked, or expired.", ErrorType.Unauthorized);

    public static readonly Error InvalidOtp =
        new("Auth.InvalidOtp", "The verification code is invalid.", ErrorType.Validation);

    public static readonly Error ExpiredOtp =
        new("Auth.ExpiredOtp", "The verification code has expired.", ErrorType.Validation);

    public static readonly Error TooManyOtpAttempts =
        new("Auth.TooManyOtpAttempts", "Too many failed verification attempts. Request a new code.", ErrorType.Forbidden);

    public static readonly Error OtpNotRequested =
        new("Auth.OtpNotRequested", "No active verification code was requested for this email.", ErrorType.Validation);

    public static readonly Error PasswordChangeFailed =
        new("Auth.PasswordChangeFailed", "Could not update the password.", ErrorType.Problem);

    public static readonly Error EmailNotConfirmed =
        new("Auth.EmailNotConfirmed", "Confirm your e-mail address before signing in.", ErrorType.Unauthorized);
}