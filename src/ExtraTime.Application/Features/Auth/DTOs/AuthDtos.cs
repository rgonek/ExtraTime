namespace ExtraTime.Application.Features.Auth.DTOs;

public sealed record RegisterRequest(string Email, string Username, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public sealed record UserDto(Guid Id, string Email, string Username, string Role);
