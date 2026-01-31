using ExtraTime.Application.Features.Auth;
using ExtraTime.Application.Features.Auth.Queries.GetCurrentUser;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Application.Features.Auth;

[TestCategory(TestCategories.Significant)]
public sealed class GetCurrentUserQueryIntegrationTests : IntegrationTestBase
{
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
}
