using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.Commands.RefreshToken;
using ExtraTime.Application.Features.Auth.DTOs;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Auth.Handlers;

public sealed class RefreshTokenCommandHandlerTests : HandlerTestBase
{
    private readonly ITokenService _tokenService;
    private readonly RefreshTokenCommandHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public RefreshTokenCommandHandlerTests()
    {
        _tokenService = Substitute.For<ITokenService>();
        _handler = new RefreshTokenCommandHandler(Context, _tokenService);
    }

    [Before(Test)]
    public void Setup()
    {
        Clock.Current = new FakeClock(_now);
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task Handle_ValidToken_ReturnsSuccess()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@example.com").Build();
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "valid-refresh-token",
            ExpiresAt = _now.AddDays(7),
            CreatedAt = _now.AddDays(-1),
            UserId = user.Id,
            User = user
        };

        var command = new RefreshTokenCommand("valid-refresh-token");

        var refreshTokens = new List<RefreshToken> { existingToken }.AsQueryable();
        var mockRefreshTokens = CreateMockDbSet(refreshTokens);
        Context.RefreshTokens.Returns(mockRefreshTokens);

        _tokenService.GenerateRefreshToken().Returns("new-refresh-token");
        _tokenService.GetRefreshTokenExpiration().Returns(_now.AddDays(7));
        _tokenService.GenerateAccessToken(user).Returns("new-access-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.AccessToken).IsEqualTo("new-access-token");
        await Assert.That(result.Value!.RefreshToken).IsEqualTo("new-refresh-token");
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid-token");
        var mockRefreshTokens = CreateMockDbSet(new List<RefreshToken>().AsQueryable());
        Context.RefreshTokens.Returns(mockRefreshTokens);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@example.com").Build();
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "expired-token",
            ExpiresAt = _now.AddDays(-1), // Expired
            CreatedAt = _now.AddDays(-8),
            UserId = user.Id,
            User = user
        };

        var command = new RefreshTokenCommand("expired-token");

        var refreshTokens = new List<RefreshToken> { expiredToken }.AsQueryable();
        var mockRefreshTokens = CreateMockDbSet(refreshTokens);
        Context.RefreshTokens.Returns(mockRefreshTokens);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_RevokedToken_ReturnsFailure()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@example.com").Build();
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "revoked-token",
            ExpiresAt = _now.AddDays(7),
            CreatedAt = _now.AddDays(-1),
            RevokedAt = _now.AddHours(-1), // Already revoked
            UserId = user.Id,
            User = user
        };

        var command = new RefreshTokenCommand("revoked-token");

        var refreshTokens = new List<RefreshToken> { revokedToken }.AsQueryable();
        var mockRefreshTokens = CreateMockDbSet(refreshTokens);
        Context.RefreshTokens.Returns(mockRefreshTokens);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
