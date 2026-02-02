using ExtraTime.Application.Features.Auth.Queries.GetCurrentUser;
using ExtraTime.Domain.Common;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Auth.Handlers;

[NotInParallel]
public sealed class GetCurrentUserQueryHandlerTests : HandlerTestBase
{
    private readonly GetCurrentUserQueryHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public GetCurrentUserQueryHandlerTests()
    {
        _handler = new GetCurrentUserQueryHandler(Context, CurrentUserService);
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
    public async Task Handle_AuthenticatedUser_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder()
            .WithId(userId)
            .WithEmail("test@example.com")
            .WithUsername("testuser")
            .Build();

        SetCurrentUser(userId, user.Email);

        var users = new List<Domain.Entities.User> { user }.AsQueryable();
        var mockUsers = CreateMockDbSet(users);
        Context.Users.Returns(mockUsers);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Id).IsEqualTo(userId);
        await Assert.That(result.Value!.Email).IsEqualTo("test@example.com");
        await Assert.That(result.Value!.Username).IsEqualTo("testuser");
    }

    [Test]
    public async Task Handle_NotAuthenticated_ReturnsFailure()
    {
        // Arrange
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetCurrentUser(userId);

        var mockUsers = CreateMockDbSet(new List<Domain.Entities.User>().AsQueryable());
        Context.Users.Returns(mockUsers);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
