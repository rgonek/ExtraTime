using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.DTOs;
using ExtraTime.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async ValueTask<Result<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existingToken = await context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingToken is null)
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        // Check if token is valid for use
        if (!existingToken.IsValidForUse())
        {
            // Check if token was already used (token reuse detection)
            if (existingToken.IsRevoked)
            {
                // Revoke all active tokens for this user (potential token theft)
                // IsActive = !IsRevoked && !IsExpired, expanded here for LINQ translation
                var now = Clock.UtcNow;
                var userTokens = await context.RefreshTokens
                    .Where(rt => rt.UserId == existingToken.UserId && rt.RevokedAt == null && rt.ExpiresAt > now)
                    .ToListAsync(cancellationToken);

                foreach (var token in userTokens)
                {
                    token.Revoke(reason: "Token reuse detected - potential theft");
                }

                await context.SaveChangesAsync(cancellationToken);
                return Result<AuthResponse>.Failure(AuthErrors.TokenReused);
            }

            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = existingToken.User;

        // Replace old token with new one using domain method
        var newRefreshToken = existingToken.ReplaceWith(
            tokenService.GenerateRefreshToken(),
            tokenService.GetRefreshTokenExpiration());

        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: newRefreshToken.Token,
            ExpiresAt: newRefreshToken.ExpiresAt,
            User: new UserDto(user.Id, user.Email, user.Username, user.Role)));
    }
}
