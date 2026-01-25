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
        var normalizedEmail = request.Email.ToLowerInvariant();

        var emailExists = await context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

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

        var user = new User
        {
            Email = normalizedEmail,
            Username = request.Username,
            PasswordHash = passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        var refreshToken = new RefreshTokenEntity
        {
            Token = tokenService.GenerateRefreshToken(),
            ExpiresAt = tokenService.GetRefreshTokenExpiration(),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        user.RefreshTokens.Add(refreshToken);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresAt: refreshToken.ExpiresAt,
            User: new UserDto(user.Id, user.Email, user.Username, user.Role)));
    }
}
