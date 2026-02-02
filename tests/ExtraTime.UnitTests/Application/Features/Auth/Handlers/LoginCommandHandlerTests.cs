using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.Commands.Login;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Auth.Handlers;

[NotInParallel]
public sealed class LoginCommandHandlerTests : HandlerTestBase
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly LoginCommandHandler _handler;
    private readonly DateTime _now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public LoginCommandHandlerTests()
    {
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();
        _handler = new LoginCommandHandler(Context, _passwordHasher, _tokenService);
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
    public async Task Handle_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var password = "Password123!";
        var user = new UserBuilder().WithEmail("test@example.com").Build();
        var command = new LoginCommand(user.Email, password);

        var users = new List<User> { user }.AsQueryable();
        var mockUsers = CreateMockDbSet(users);
        Context.Users.Returns(mockUsers);
        
        var mockTokens = CreateMockDbSet(new List<RefreshToken>().AsQueryable());
        Context.RefreshTokens.Returns(mockTokens);

        _passwordHasher.Verify(password, user.PasswordHash).Returns(true);
        _tokenService.GenerateRefreshToken().Returns("refresh-token");
        _tokenService.GetRefreshTokenExpiration().Returns(_now.AddDays(7));
        _tokenService.GenerateAccessToken(user).Returns("access-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.AccessToken).IsEqualTo("access-token");
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_InvalidUser_ReturnsFailure()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "any");
        var mockUsers = CreateMockDbSet(new List<User>().AsQueryable());
        Context.Users.Returns(mockUsers);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("test@example.com").Build();
        var command = new LoginCommand(user.Email, "wrong");

        var users = new List<User> { user }.AsQueryable();
        var mockUsers = CreateMockDbSet(users);
        Context.Users.Returns(mockUsers);

        _passwordHasher.Verify("wrong", user.PasswordHash).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
