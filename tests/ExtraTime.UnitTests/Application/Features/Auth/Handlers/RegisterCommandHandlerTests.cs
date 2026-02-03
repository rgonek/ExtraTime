using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.Commands.Register;
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
public sealed class RegisterCommandHandlerTests : HandlerTestBase
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly RegisterCommandHandler _handler;
    private readonly DateTime _now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public RegisterCommandHandlerTests()
    {
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();
        _handler = new RegisterCommandHandler(Context, _passwordHasher, _tokenService);
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
    public async Task Handle_ValidRegistration_ReturnsSuccess()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "newuser", "Password123!");

        var mockUsers = CreateMockDbSet(new List<User>().AsQueryable());
        Context.Users.Returns(mockUsers);

        _passwordHasher.Hash(command.Password).Returns("hashed-password");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");
        _tokenService.GetRefreshTokenExpiration().Returns(_now.AddDays(7));
        _tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        Context.Users.Received(1).Add(Arg.Is<User>(u =>
            u.Email == command.Email && u.Username == command.Username));
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = new UserBuilder().WithEmail("exists@example.com").Build();
        var command = new RegisterCommand(existingUser.Email, "other", "Password123!");

        var users = new List<User> { existingUser }.AsQueryable();
        var mockUsers = CreateMockDbSet(users);
        Context.Users.Returns(mockUsers);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        Context.Users.DidNotReceive().Add(Arg.Any<User>());
    }
}
