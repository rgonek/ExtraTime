using ExtraTime.Application.Features.Auth.Commands.RefreshToken;
using ExtraTime.Domain.Entities;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExtraTime.IntegrationTests.Application.Features.Auth;

public sealed class RefreshTokenCommandIntegrationTests : IntegrationTestBase
{
    private readonly TokenService _tokenService;

    public RefreshTokenCommandIntegrationTests()
    {
        var jwtSettings = new JwtSettings
        {
            Secret = "your-256-bit-secret-key-for-testing-purposes-only-12345678901234567890",
            Issuer = "ExtraTime.Test",
            Audience = "ExtraTime.Test",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
        _tokenService = new TokenService(Options.Create(jwtSettings));
    }

    [Test]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var user = User.Register("test@example.com", "testuser", "hashedpassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Token = "valid-refresh-token-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };
        Context.RefreshTokens.Add(refreshToken);
        await Context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(Context, _tokenService);
        var command = new RefreshTokenCommand(refreshToken.Token);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.AccessToken).IsNotNullOrEmpty();
        await Assert.That(result.Value.RefreshToken).IsNotNullOrEmpty();
        await Assert.That(result.Value.RefreshToken).IsNotEqualTo(refreshToken.Token); // New token generated

        // Verify old token is revoked
        var oldToken = await Context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);
        await Assert.That(oldToken!.RevokedAt).IsNotNull();
        await Assert.That(oldToken.ReplacedByToken).IsEqualTo(result.Value.RefreshToken);

        // Verify new token exists
        var newToken = await Context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == result.Value.RefreshToken);
        await Assert.That(newToken).IsNotNull();
        await Assert.That(newToken!.UserId).IsEqualTo(user.Id);
    }

    [Test]
    public async Task RefreshToken_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var handler = new RefreshTokenCommandHandler(Context, _tokenService);
        var command = new RefreshTokenCommand("invalid-token");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task RefreshToken_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var user = User.Register("test@example.com", "testuser", "hashedpassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Token = "expired-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            UserId = user.Id
        };
        Context.RefreshTokens.Add(refreshToken);
        await Context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(Context, _tokenService);
        var command = new RefreshTokenCommand(refreshToken.Token);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task RefreshToken_ReusedToken_RevokesAllUserTokens()
    {
        // Arrange
        var user = User.Register("test@example.com", "testuser", "hashedpassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Create multiple active tokens
        var token1 = new RefreshToken
        {
            Token = "valid-token-1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };
        var token2 = new RefreshToken
        {
            Token = "valid-token-2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };
        var reusedToken = new RefreshToken
        {
            Token = "reused-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id,
            RevokedAt = DateTime.UtcNow // Already revoked
        };

        Context.RefreshTokens.AddRange(token1, token2, reusedToken);
        await Context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(Context, _tokenService);
        var command = new RefreshTokenCommand(reusedToken.Token);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();

        // Verify all tokens are revoked
        var allTokens = await Context.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();
        foreach (var token in allTokens)
        {
            await Assert.That(token.RevokedAt).IsNotNull();
        }
    }
}
