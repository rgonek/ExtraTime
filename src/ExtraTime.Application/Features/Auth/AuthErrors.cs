namespace ExtraTime.Application.Features.Auth;

public static class AuthErrors
{
    public const string EmailAlreadyExists = "A user with this email already exists.";
    public const string UsernameAlreadyExists = "A user with this username already exists.";
    public const string InvalidCredentials = "Invalid email or password.";
    public const string InvalidRefreshToken = "Invalid or expired refresh token.";
    public const string TokenReused = "Refresh token reuse detected. All tokens have been revoked.";
    public const string UserNotFound = "User not found.";
}
