namespace ECommerce.Application.Auth;

public sealed record EmailSentResponse(
    string Email,
    bool VerificationCodeResent,
    string Message);