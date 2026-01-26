using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = ExtraTime.Domain.Entities.RefreshToken;

namespace ExtraTime.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async ValueTask<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        user.UpdateLastLogin();

        var refreshTokenString = tokenService.GenerateRefreshToken();
        var expiresAt = tokenService.GetRefreshTokenExpiration();

        user.AddRefreshToken(refreshTokenString, expiresAt);

        await context.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = user.RefreshTokens.First(t => t.Token == refreshTokenString);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresAt: refreshToken.ExpiresAt,
            User: new UserDto(user.Id, user.Email, user.Username, user.Role)));
    }
}
