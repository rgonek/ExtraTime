using ExtraTime.Application.Features.Auth.Commands.Register;
using ExtraTime.Domain.Entities;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExtraTime.IntegrationTests.Application.Features.Auth;

public sealed class RegisterCommandIntegrationTests : IntegrationTestBase
{
    private readonly PasswordHasher _passwordHasher = new();
    private readonly TokenService _tokenService;

    public RegisterCommandIntegrationTests()
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
    public async Task Register_ValidData_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var email = "newuser@example.com";
        var username = "newuser";
        var password = "SecurePassword123!";

        var handler = new RegisterCommandHandler(Context, _passwordHasher, _tokenService);
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
        await Assert.That(user.Role).IsEqualTo(Domain.Enums.UserRole.User);
        await Assert.That(user.CreatedAt).IsNotDefault();
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var email = "existing@example.com";
        var existingUser = User.Register(email, "existinguser", _passwordHasher.Hash("password123"), Domain.Enums.UserRole.User);
        Context.Users.Add(existingUser);
        await Context.SaveChangesAsync();

        var handler = new RegisterCommandHandler(Context, _passwordHasher, _tokenService);
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
        var existingUser = User.Register("existing@example.com", username, _passwordHasher.Hash("password123"), Domain.Enums.UserRole.User);
        Context.Users.Add(existingUser);
        await Context.SaveChangesAsync();

        var handler = new RegisterCommandHandler(Context, _passwordHasher, _tokenService);
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
        var existingUser = User.Register(lowerEmail, "existinguser", _passwordHasher.Hash("password123"), Domain.Enums.UserRole.User);
        Context.Users.Add(existingUser);
        await Context.SaveChangesAsync();

        var handler = new RegisterCommandHandler(Context, _passwordHasher, _tokenService);
        var command = new RegisterCommand(email, "newuser", "SecurePassword123!");

        // Act
        var result = await handler.Handle(command, default);

        // Assert - should fail because email matches case-insensitively
        await Assert.That(result.IsFailure).IsTrue();
    }
}
