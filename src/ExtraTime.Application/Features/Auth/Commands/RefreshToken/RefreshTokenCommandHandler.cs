using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.DTOs;
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

        // Check if token was already used (token reuse detection)
        if (existingToken.RevokedAt is not null)
        {
            // Revoke all tokens for this user (potential token theft)
            var userTokens = await context.RefreshTokens
                .Where(rt => rt.UserId == existingToken.UserId && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in userTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result<AuthResponse>.Failure(AuthErrors.TokenReused);
        }

        // Check if token is expired
        if (existingToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = existingToken.User;

        // Create new refresh token
        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            Token = tokenService.GenerateRefreshToken(),
            ExpiresAt = tokenService.GetRefreshTokenExpiration(),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        // Revoke old token and link to new one
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshToken.Token;

        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: newRefreshToken.Token,
            ExpiresAt: newRefreshToken.ExpiresAt,
            User: new UserDto(user.Id, user.Email, user.Username, user.Role.ToString())));
    }
}
