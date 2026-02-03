using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues;
using ExtraTime.Application.Features.Leagues.Commands.JoinLeague;
using ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Leagues;

public sealed class RegenerateInviteCodeTests : IntegrationTestBase
{
    [Test]
    [TestCategory(TestCategories.Significant)]
    public async Task RegenerateInviteCode_GeneratesNewCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var originalCode = "ORIGINAL";
        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithInviteCode(originalCode)
            .Build();
        Context.Leagues.Add(league);

        await Context.SaveChangesAsync();
        SetCurrentUser(userId);

        var inviteCodeGenerator = new InviteCodeGenerator();
        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);

        var command = new RegenerateInviteCodeCommand(league.Id, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value.InviteCode).IsNotNull();
        await Assert.That(result.Value.InviteCode).IsNotEqualTo(originalCode);
        await Assert.That(result.Value.InviteCode.Length).IsEqualTo(8);

        // Verify persistence
        var updatedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(updatedLeague).IsNotNull();
        await Assert.That(updatedLeague!.InviteCode).IsNotEqualTo(originalCode);
        await Assert.That(updatedLeague.InviteCode.Length).IsEqualTo(8);
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    public async Task RegenerateInviteCode_NotOwner_ReturnsForbidden()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();
        Context.Users.Add(otherUser);

        var originalCode = "LEAGUE01";
        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode(originalCode)
            .Build();
        Context.Leagues.Add(league);

        // Add other user as member
        var member = LeagueMember.Create(league.Id, otherUserId, MemberRole.Member);
        Context.LeagueMembers.Add(member);

        await Context.SaveChangesAsync();
        SetCurrentUser(otherUserId); // Set as non-owner member

        var inviteCodeGenerator = new InviteCodeGenerator();
        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);

        var command = new RegenerateInviteCodeCommand(league.Id, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(LeagueErrors.NotTheOwner);

        // Verify code was not changed
        var unchangedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(unchangedLeague!.InviteCode).IsEqualTo(originalCode);
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    public async Task RegenerateInviteCode_OldCodeInvalid()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);

        var oldInviteCode = "OLD12345";
        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode(oldInviteCode)
            .Build();
        Context.Leagues.Add(league);

        await Context.SaveChangesAsync();
        SetCurrentUser(ownerId);

        var inviteCodeGenerator = new InviteCodeGenerator();
        var regenerateHandler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);

        // First, regenerate the invite code
        var regenerateCommand = new RegenerateInviteCodeCommand(league.Id, null);
        var regenerateResult = await regenerateHandler.Handle(regenerateCommand, default);

        await Assert.That(regenerateResult.IsSuccess).IsTrue();
        var newInviteCode = regenerateResult.Value.InviteCode;

        // Now try to join with the old code
        SetCurrentUser(newUserId);
        var joinHandler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var joinCommand = new JoinLeagueCommand(league.Id, oldInviteCode);

        // Act - Try joining with old code
        var joinResult = await joinHandler.Handle(joinCommand, default);

        // Assert
        await Assert.That(joinResult.IsFailure).IsTrue();
        await Assert.That(joinResult.Error).IsEqualTo(LeagueErrors.InvalidInviteCode);

        // Verify the user is NOT a member
        var membership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == league.Id && m.UserId == newUserId);
        await Assert.That(membership).IsNull();

        // Now try joining with the new code
        var joinCommandWithNewCode = new JoinLeagueCommand(league.Id, newInviteCode);
        var joinResultWithNewCode = await joinHandler.Handle(joinCommandWithNewCode, default);

        await Assert.That(joinResultWithNewCode.IsSuccess).IsTrue();

        // Verify the user is now a member
        var newMembership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == league.Id && m.UserId == newUserId);
        await Assert.That(newMembership).IsNotNull();
    }
}
