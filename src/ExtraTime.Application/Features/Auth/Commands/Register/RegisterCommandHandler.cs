using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;
using User = ExtraTime.Domain.Entities.User;
using RefreshTokenEntity = ExtraTime.Domain.Entities.RefreshToken;

namespace ExtraTime.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    public async ValueTask<Result<AuthResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var emailExists = await context.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
        {
            return Result<AuthResponse>.Failure(AuthErrors.EmailAlreadyExists);
        }

        var usernameExists = await context.Users
            .AnyAsync(u => u.Username == request.Username, cancellationToken);

        if (usernameExists)
        {
            return Result<AuthResponse>.Failure(AuthErrors.UsernameAlreadyExists);
        }

        var user = User.Register(
            request.Email,
            request.Username,
            passwordHasher.Hash(request.Password));

        var refreshTokenString = tokenService.GenerateRefreshToken();
        var expiresAt = tokenService.GetRefreshTokenExpiration();

        user.AddRefreshToken(refreshTokenString, expiresAt);

        context.Users.Add(user);
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
