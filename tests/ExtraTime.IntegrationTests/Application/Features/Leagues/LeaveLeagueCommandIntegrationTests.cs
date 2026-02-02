using ExtraTime.Application.Features.Leagues.Commands.LeaveLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

[TestCategory(TestCategories.RequiresDatabase)]
public sealed class LeaveLeagueCommandIntegrationTests : IntegrationTestBase
{
    protected override bool UseSqlDatabase => true;
    [Test]
    public async Task LeaveLeague_AsMember_RemovesSelf()
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

        league.AddMember(memberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        // Detach league to avoid concurrency issues in InMemory database
        Context.Entry(league).State = EntityState.Detached;

        SetCurrentUser(memberId);

        var handler = new LeaveLeagueCommandHandler(Context, CurrentUserService);
        var command = new LeaveLeagueCommand(league.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var membership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == league.Id && m.UserId == memberId);

        await Assert.That(membership).IsNull();
    }

    [Test]
    public async Task LeaveLeague_OwnerCannotLeave_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        // Owner is added as Owner member
        league.AddMember(ownerId, MemberRole.Owner);
        await Context.SaveChangesAsync();

        // Detach league to avoid concurrency issues in InMemory database
        Context.Entry(league).State = EntityState.Detached;

        SetCurrentUser(ownerId);

        var handler = new LeaveLeagueCommandHandler(Context, CurrentUserService);
        var command = new LeaveLeagueCommand(league.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();

        // Verify owner is still a member
        var membership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == league.Id && m.UserId == ownerId);

        await Assert.That(membership).IsNotNull();
    }

    [Test]
    public async Task LeaveLeague_NotAMember_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var nonMemberId = Guid.NewGuid();
        var nonMember = new UserBuilder().WithId(nonMemberId).Build();
        Context.Users.Add(nonMember);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(nonMemberId); // Not a member

        var handler = new LeaveLeagueCommandHandler(Context, CurrentUserService);
        var command = new LeaveLeagueCommand(league.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task LeaveLeague_NonExistentLeague_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new LeaveLeagueCommandHandler(Context, CurrentUserService);
        var command = new LeaveLeagueCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
