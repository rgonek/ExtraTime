using ExtraTime.Application.Features.Leagues.Commands.DeleteLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

[TestCategory(TestCategories.Significant)]
public sealed class DeleteLeagueCommandIntegrationTests : IntegrationTestBase
{
    [Test]
    [TestCategory(TestCategories.RequiresDatabase)]
    public async Task DeleteLeague_AsOwner_DeletesLeagueAndMembers()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var memberId = Guid.NewGuid();
        var member = new UserBuilder().WithId(memberId).Build();
        Context.Users.Add(member);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        // Add a member
        league.AddMember(memberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        // Reload league to avoid concurrency issues with InMemory database
        Context.Entry(league).State = EntityState.Detached;

        SetCurrentUser(ownerId);

        var handler = new DeleteLeagueCommandHandler(Context, CurrentUserService);
        var command = new DeleteLeagueCommand(league.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var deletedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(deletedLeague).IsNull();

        var memberships = await Context.LeagueMembers
            .Where(m => m.LeagueId == league.Id)
            .ToListAsync();
        await Assert.That(memberships.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DeleteLeague_NotOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();
        Context.Users.Add(otherUser);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(otherUserId);

        var handler = new DeleteLeagueCommandHandler(Context, CurrentUserService);
        var command = new DeleteLeagueCommand(league.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();

        // Verify league still exists
        var existingLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(existingLeague).IsNotNull();
    }

    [Test]
    public async Task DeleteLeague_NonExistent_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new DeleteLeagueCommandHandler(Context, CurrentUserService);
        var command = new DeleteLeagueCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
