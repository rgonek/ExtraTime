using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth;
using ExtraTime.Application.Features.Auth.Commands.Login;
using ExtraTime.Application.Features.Auth.Commands.RefreshToken;
using ExtraTime.Application.Features.Auth.Commands.Register;
using ExtraTime.Application.Features.Auth.Queries.GetCurrentUser;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Auth;

public sealed class AuthTests : IntegrationTestBase
{
    private readonly PasswordHasher _passwordHasher = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly TokenService _realTokenService;

    public AuthTests()
    {
        var jwtSettings = new JwtSettings
        {
            Secret = "your-256-bit-secret-key-for-testing-purposes-only-12345678901234567890",
            Issuer = "ExtraTime.Test",
            Audience = "ExtraTime.Test",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
        _realTokenService = new TokenService(Options.Create(jwtSettings));
    }

    //
    // Register Tests
    //

    [Test]
    public async Task Register_ValidData_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var email = "newuser@example.com";
        var username = "newuser";
        var password = "SecurePassword123!";

        var handler = new RegisterCommandHandler(Context, _passwordHasher, _realTokenService);
        var command = new RegisterCommand(email, username, password);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.AccessToken).IsNotNullOrEmpty();
        await Assert.That(result.Value.RefreshToken).IsNotNullOrEmpty();
        await Assert.That(result.Value.User.Email).IsEqualTo(email.ToLowerInvariant());
        await Assert.That(result.Value.User.Username).IsEqualTo(username);

        // Verify user was persisted
        var user = await Context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

        await Assert.That(user).IsNotNull();
        await Assert.That(user!.Username).IsEqualTo(username);
        await Assert.That(_passwordHasher.Verify(password, user.PasswordHash)).IsTrue();
        await Assert.That(user.RefreshTokens.Count).IsEqualTo(1);
        await Assert.That(user.Role).IsEqualTo(UserRole.User);
        await Assert.That(user.CreatedAt).IsNotDefault();
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var email = "existing@example.com";
        var existingUser = User.Register(email, "existinguser", _passwordHasher.Hash("password123"), UserRole.User);
        Context.Users.Add(existingUser);
        await Context.SaveChangesAsync();

        var handler = new RegisterCommandHandler(Context, _passwordHasher, _realTokenService);
        var command = new RegisterCommand(email, "newuser", "SecurePassword123!");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Register_DuplicateUsername_ReturnsFailure()
    {
        // Arrange
        var username = "existinguser";
        var existingUser = User.Register("existing@example.com", username, _passwordHasher.Hash("password123"), UserRole.User);
        Context.Users.Add(existingUser);
        await Context.SaveChangesAsync();

        var handler = new RegisterCommandHandler(Context, _passwordHasher, _realTokenService);
        var command = new RegisterCommand("new@example.com", username, "SecurePassword123!");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Register_EmailCaseInsensitive_ChecksForDuplicates()
    {
        // Arrange
        var email = "User@Example.COM";
        var lowerEmail = email.ToLowerInvariant();
        var existingUser = User.Register(lowerEmail, "existinguser", _passwordHasher.Hash("password123"), UserRole.User);
        Context.Users.Add(existingUser);
        await Context.SaveChangesAsync();

        var handler = new RegisterCommandHandler(Context, _passwordHasher, _realTokenService);
        var command = new RegisterCommand(email, "newuser", "SecurePassword123!");

        // Act
        var result = await handler.Handle(command, default);

        // Assert - should fail because email matches case-insensitively
        await Assert.That(result.IsFailure).IsTrue();
    }

    //
    // Login Tests
    //

    [Test]
    public async Task Login_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var email = "test@example.com";
        var password = "TestPassword123!";
        var passwordHash = _passwordHasher.Hash(password);

        var user = new UserBuilder()
            .WithEmail(email)
            .WithPasswordHash(passwordHash)
            .Build();

        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Clear tracker to ensure handler loads fresh entity
        Context.ChangeTracker.Clear();

        var handler = new LoginCommandHandler(Context, _passwordHasher, _realTokenService);
        var command = new LoginCommand(email, password);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.User.Email).IsEqualTo(email);
    }

    [Test]
    public async Task Login_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var password = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        var passwordHash = _passwordHasher.Hash(password);

        var user = new UserBuilder()
            .WithEmail(email)
            .WithPasswordHash(passwordHash)
            .Build();

        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Clear tracker to ensure handler loads fresh entity
        Context.ChangeTracker.Clear();

        var handler = new LoginCommandHandler(Context, _passwordHasher, _realTokenService);
        var command = new LoginCommand(email, wrongPassword);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Login_EmailCaseInsensitive_ReturnsSuccess()
    {
        // Arrange
        var email = "Test.User@Example.COM";
        var lowerEmail = email.ToLowerInvariant();
        var password = "TestPassword123!";
        var passwordHash = _passwordHasher.Hash(password);

        var user = new UserBuilder()
            .WithEmail(lowerEmail)
            .WithPasswordHash(passwordHash)
            .Build();

        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Clear tracker to ensure handler loads fresh entity
        Context.ChangeTracker.Clear();

        var handler = new LoginCommandHandler(Context, _passwordHasher, _realTokenService);
        var command = new LoginCommand(email, password); // Use mixed case email

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.User.Email).IsEqualTo(lowerEmail);
    }

    //
    // RefreshToken Tests
    //

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

    //
    // GetCurrentUser Tests
    //

    [Test]
    public async Task GetCurrentUser_AuthenticatedUser_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder()
            .WithId(userId)
            .WithEmail("test@example.com")
            .WithUsername("testuser")
            .WithRole(UserRole.User)
            .Build();

        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetCurrentUserQueryHandler(Context, CurrentUserService);
        var query = new GetCurrentUserQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Id).IsEqualTo(userId);
        await Assert.That(result.Value.Email).IsEqualTo("test@example.com");
        await Assert.That(result.Value.Username).IsEqualTo("testuser");
        await Assert.That(result.Value.Role).IsEqualTo(UserRole.User);
    }

    [Test]
    public async Task GetCurrentUser_NotAuthenticated_ReturnsFailure()
    {
        // Arrange
        // Do not set current user - IsAuthenticated is false by default on mock
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null!);

        var handler = new GetCurrentUserQueryHandler(Context, CurrentUserService);
        var query = new GetCurrentUserQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AuthErrors.UserNotFound);
    }

    [Test]
    public async Task GetCurrentUser_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        SetCurrentUser(nonExistentUserId);

        var handler = new GetCurrentUserQueryHandler(Context, CurrentUserService);
        var query = new GetCurrentUserQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AuthErrors.UserNotFound);
    }

    public class FakeTokenService : ITokenService
    {
        private int _tokenCounter = 0;

        public string GenerateAccessToken(User user) => $"access-token-{user.Id}-{_tokenCounter++}";

        public string GenerateRefreshToken() => $"refresh-token-{_tokenCounter++}-{Guid.NewGuid()}";

        public DateTime GetRefreshTokenExpiration() => DateTime.UtcNow.AddDays(7);
    }
}
