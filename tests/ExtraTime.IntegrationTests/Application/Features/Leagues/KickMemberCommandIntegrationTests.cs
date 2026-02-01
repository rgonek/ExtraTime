using ExtraTime.Application.Features.Leagues.Commands.KickMember;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

using ExtraTime.IntegrationTests.Attributes;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

[TestCategory(TestCategories.RequiresDatabase)]
public sealed class KickMemberCommandIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task KickMember_AsOwner_RemovesMember()
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

        SetCurrentUser(ownerId);

        var handler = new KickMemberCommandHandler(Context, CurrentUserService);
        var command = new KickMemberCommand(league.Id, memberId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var membership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == league.Id && m.UserId == memberId);

        await Assert.That(membership).IsNull();
    }

    [Test]
    public async Task KickMember_NotOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var memberId = Guid.NewGuid();
        var member = new UserBuilder().WithId(memberId).Build();
        Context.Users.Add(member);

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();
        Context.Users.Add(otherUser);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        league.AddMember(memberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        // Detach league to avoid concurrency issues in InMemory database
        Context.Entry(league).State = EntityState.Detached;

        SetCurrentUser(otherUserId); // Not the owner

        var handler = new KickMemberCommandHandler(Context, CurrentUserService);
        var command = new KickMemberCommand(league.Id, memberId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();

        // Verify member still exists
        var membership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == league.Id && m.UserId == memberId);

        await Assert.That(membership).IsNotNull();
    }

    [Test]
    public async Task KickMember_CannotKickOwner_ReturnsFailure()
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

        // Add owner as a member with Owner role
        league.AddMember(ownerId, MemberRole.Owner);
        await Context.SaveChangesAsync();

        // Detach league to avoid concurrency issues in InMemory database
        Context.Entry(league).State = EntityState.Detached;

        SetCurrentUser(ownerId);

        var handler = new KickMemberCommandHandler(Context, CurrentUserService);
        var command = new KickMemberCommand(league.Id, ownerId); // Try to kick self

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task KickMember_NonExistentMember_ReturnsFailure()
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

        SetCurrentUser(ownerId);

        var handler = new KickMemberCommandHandler(Context, CurrentUserService);
        var command = new KickMemberCommand(league.Id, Guid.NewGuid()); // Non-existent user

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
