using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.Commands.RefreshToken;
using ExtraTime.Application.Features.Auth.DTOs;
using ExtraTime.Domain.Entities;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Auth;

public class RefreshTokenCommandIntegrationTests : IntegrationTestBase
{
    private readonly FakeTokenService _tokenService = new();

    [Test]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var user = User.Register("test@example.com", "testuser", "hashedpassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var refreshToken = new RefreshTokenBuilder()
            .WithToken("valid-refresh-token-123")
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .WithUserId(user.Id)
            .Build();
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

        var refreshToken = new RefreshTokenBuilder()
            .WithToken("expired-refresh-token")
            .WithExpiresAt(DateTime.UtcNow.AddDays(-1)) // Expired
            .WithUserId(user.Id)
            .Build();
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
    public async Task RefreshToken_RevokedToken_ReturnsFailureAndRevokesAllUserTokens()
    {
        // Arrange
        var user = User.Register("test@example.com", "testuser", "hashedpassword123");
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Create a revoked token
        var revokedToken = new RefreshTokenBuilder()
            .WithToken("revoked-token")
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .WithUserId(user.Id)
            .Build();
        revokedToken.Revoke(reason: "Test revocation");
        Context.RefreshTokens.Add(revokedToken);

        // Create an active token for the same user
        var activeToken = new RefreshTokenBuilder()
            .WithToken("active-token")
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .WithUserId(user.Id)
            .Build();
        Context.RefreshTokens.Add(activeToken);

        await Context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(Context, _tokenService);
        var command = new RefreshTokenCommand(revokedToken.Token);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();

        // Verify all user tokens are now revoked (token reuse detection)
        var allTokens = await Context.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();
        foreach (var token in allTokens)
        {
            await Assert.That(token.IsRevoked || token.IsExpired).IsTrue();
        }
    }
}

public class FakeTokenService : ITokenService
{
    private int _tokenCounter = 0;

    public string GenerateAccessToken(User user) => $"access-token-{user.Id}-{_tokenCounter++}";

    public string GenerateRefreshToken() => $"refresh-token-{_tokenCounter++}-{Guid.NewGuid()}";

    public DateTime GetRefreshTokenExpiration() => DateTime.UtcNow.AddDays(7);
}
