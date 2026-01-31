using ExtraTime.Application.Features.Auth.Commands.Login;
using ExtraTime.Domain.Entities;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExtraTime.IntegrationTests.Application.Features.Auth;

[TestCategory(TestCategories.Significant)]
public sealed class LoginCommandIntegrationTests : IntegrationTestBase
{
    private readonly PasswordHasher _passwordHasher = new();
    private readonly TokenService _tokenService;

    public LoginCommandIntegrationTests()
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

        var handler = new LoginCommandHandler(Context, _passwordHasher, _tokenService);
        var command = new LoginCommand(email, password);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.AccessToken).IsNotNullOrEmpty();
        await Assert.That(result.Value.RefreshToken).IsNotNullOrEmpty();
        await Assert.That(result.Value.User.Email).IsEqualTo(email.ToLowerInvariant());
        await Assert.That(result.Value.User.Id).IsEqualTo(user.Id);

        // Verify refresh token was persisted
        var userWithTokens = await Context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        await Assert.That(userWithTokens!.RefreshTokens.Count).IsEqualTo(1);
        await Assert.That(userWithTokens.RefreshTokens.First().Token).IsEqualTo(result.Value.RefreshToken);
        await Assert.That(userWithTokens.LastLoginAt).IsNotNull();
    }

    [Test]
    public async Task Login_InvalidEmail_ReturnsFailure()
    {
        // Arrange
        var handler = new LoginCommandHandler(Context, _passwordHasher, _tokenService);
        var command = new LoginCommand("nonexistent@example.com", "password123");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
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

        var handler = new LoginCommandHandler(Context, _passwordHasher, _tokenService);
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

        var handler = new LoginCommandHandler(Context, _passwordHasher, _tokenService);
        var command = new LoginCommand(email, password); // Use mixed case email

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.User.Email).IsEqualTo(lowerEmail);
    }
}
