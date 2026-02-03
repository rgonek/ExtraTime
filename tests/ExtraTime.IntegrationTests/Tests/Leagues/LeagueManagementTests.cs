using ExtraTime.Application.Features.Leagues.Commands.KickMember;
using ExtraTime.Application.Features.Leagues.Commands.LeaveLeague;
using ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;
using ExtraTime.Application.Features.Leagues.Commands.UpdateLeague;
using ExtraTime.Application.Features.Leagues.Queries.GetUserLeagues;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Leagues;

public sealed class LeagueManagementTests : IntegrationTestBase
{
    //
    // Kick Member Tests
    //

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

        // Add member
        // Use Include to ensure EF Core tracks the collection properly
        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(memberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        // Clear tracker to simulate fresh request
        Context.ChangeTracker.Clear();

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

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(memberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

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

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var handler = new KickMemberCommandHandler(Context, CurrentUserService);
        var command = new KickMemberCommand(league.Id, ownerId); // Try to kick self

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    //
    // Leave League Tests
    //

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

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(memberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

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

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var handler = new KickMemberCommandHandler(Context, CurrentUserService);
        var command = new KickMemberCommand(league.Id, Guid.NewGuid()); // Non-existent user

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    //
    // Leave League Tests
    //

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

        // Note: League.Create already adds the owner as a member, no need to add again
        Context.ChangeTracker.Clear();

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
    public async Task LeaveLeague_NotAMember_ReturnsSuccess()
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

        // Assert - Domain treats this as idempotent (no-op), returns success
        await Assert.That(result.IsSuccess).IsTrue();
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

    // Update League Tests
    //

    [Test]
    public async Task UpdateLeague_ValidData_UpdatesLeagueSettings()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithName("Original Name")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
        var command = new UpdateLeagueCommand(
            league.Id,
            "Updated Name",
            "Updated Description",
            true,
            20,
            5,
            2,
            10,
            null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Name).IsEqualTo("Updated Name");

        var updatedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(updatedLeague!.Name).IsEqualTo("Updated Name");
        await Assert.That(updatedLeague.Description).IsEqualTo("Updated Description");
        await Assert.That(updatedLeague.IsPublic).IsTrue();
        await Assert.That(updatedLeague.MaxMembers).IsEqualTo(20);
        await Assert.That(updatedLeague.ScoreExactMatch).IsEqualTo(5);
        await Assert.That(updatedLeague.ScoreCorrectResult).IsEqualTo(2);
        await Assert.That(updatedLeague.BettingDeadlineMinutes).IsEqualTo(10);
    }

    [Test]
    public async Task UpdateLeague_WithCompetitionFilter_UpdatesAllowedCompetitions()
    {
        // Arrange
        var competition1 = new CompetitionBuilder().Build();
        var competition2 = new CompetitionBuilder().Build();
        Context.Competitions.AddRange(competition1, competition2);

        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
        var command = new UpdateLeagueCommand(
            league.Id,
            league.Name,
            null,
            false,
            50,
            3,
            1,
            5,
            new[] { competition1.Id, competition2.Id });

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var updatedLeague = await Context.Leagues.FindAsync(league.Id);
        var allowedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(updatedLeague!.AllowedCompetitionIds!);

        await Assert.That(allowedIds).Contains(competition1.Id);
        await Assert.That(allowedIds).Contains(competition2.Id);
    }

    [Test]
    public async Task UpdateLeague_NotOwner_ReturnsFailure()
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

        Context.ChangeTracker.Clear();

        SetCurrentUser(otherUserId); // Not the owner

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
        var command = new UpdateLeagueCommand(
            league.Id,
            "Updated Name",
            null,
            false,
            50,
            3,
            1,
            5,
            null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task UpdateLeague_InvalidCompetitionId_ReturnsFailure()
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

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
        var command = new UpdateLeagueCommand(
            league.Id,
            league.Name,
            null,
            false,
            50,
            3,
            1,
            5,
            new[] { Guid.NewGuid() }); // Non-existent competition

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    //
    // Regenerate Invite Code Tests
    //

    [Test]
    public async Task RegenerateInviteCode_ValidRequest_GeneratesNewCode()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("OLD123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        inviteCodeGenerator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns("NEW456");

        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(league.Id, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.InviteCode).IsEqualTo("NEW456");
        await Assert.That(result.Value!.InviteCodeExpiresAt).IsNull();

        var updatedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(updatedLeague!.InviteCode).IsEqualTo("NEW456");
    }

    [Test]
    public async Task RegenerateInviteCode_WithExpiration_SetsExpiresAt()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("OLD123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var expiresAt = DateTime.UtcNow.AddDays(7);

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        inviteCodeGenerator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns("NEW456");

        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(league.Id, expiresAt);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.InviteCodeExpiresAt).IsEqualTo(expiresAt);

        // Verify in database
        var updatedLeague = await Context.Leagues.FirstOrDefaultAsync(l => l.Id == league.Id);
        await Assert.That(updatedLeague!.InviteCodeExpiresAt).IsEqualTo(expiresAt);
    }

    [Test]
    public async Task RegenerateInviteCode_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(Guid.NewGuid(), null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task RegenerateInviteCode_NotOwner_ReturnsFailure()
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
            .WithInviteCode("OLD123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(otherUserId); // Not the owner

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(league.Id, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task RegenerateInviteCode_GeneratesUniqueCode_ViaGenerator()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("OLD123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        inviteCodeGenerator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns("UNIQUE78");

        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(league.Id, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        // Verify the generator was called with a uniqueness check function
        await inviteCodeGenerator.Received(1)
            .GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>());
    }

    //
    // Get User Leagues Tests
    //

    [Test]
    public async Task Handle_UserWithMultipleLeagues_ReturnsAllLeagues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("testuser").Build();

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).WithUsername("otheruser").Build();

        Context.Users.AddRange(user, otherUser);
        await Context.SaveChangesAsync();

        // Create multiple leagues where user is a member
        var league1 = new LeagueBuilder()
            .WithName("League 1")
            .WithOwnerId(otherUserId)
            .Build();
        Context.Leagues.Add(league1);

        var league2 = new LeagueBuilder()
            .WithName("League 2")
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league2);

        var league3 = new LeagueBuilder()
            .WithName("League 3")
            .WithOwnerId(otherUserId)
            .Build();
        Context.Leagues.Add(league3);

        await Context.SaveChangesAsync();

        // Update timestamps manually
        league1.CreatedAt = DateTime.UtcNow.AddDays(-2);
        league2.CreatedAt = DateTime.UtcNow.AddDays(-1);
        league3.CreatedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        // Add memberships
        var member1 = new LeagueMemberBuilder()
            .WithLeagueId(league1.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .Build();

        var member3 = new LeagueMemberBuilder()
            .WithLeagueId(league3.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .Build();

        Context.LeagueMembers.AddRange(member1, member3);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(userId);

        var handler = new GetUserLeaguesQueryHandler(Context, CurrentUserService);
        var query = new GetUserLeaguesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(3);

        // Order by CreatedAt Descending
        await Assert.That(result.Value[0].Name).IsEqualTo("League 3");
        await Assert.That(result.Value[1].Name).IsEqualTo("League 2");
        await Assert.That(result.Value[2].Name).IsEqualTo("League 1");
    }

    [Test]
    public async Task GetUserLeagues_UserWithNoLeagues_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetUserLeaguesQueryHandler(Context, CurrentUserService);
        var query = new GetUserLeaguesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetUserLeagues_LeagueSummary_ContainsCorrectData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("currentuser").Build();

        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).WithUsername("leagueowner").Build();

        Context.Users.AddRange(user, owner);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithName("Test League")
            .WithOwnerId(ownerId)
            .WithPublic(true)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        // Add two more members to the league (owner membership is already created)
        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(userId, MemberRole.Member);

        var member3Id = Guid.NewGuid();
        var member3User = new UserBuilder().WithId(member3Id).Build();
        Context.Users.Add(member3User);
        await Context.SaveChangesAsync();

        trackedLeague.AddMember(member3Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetUserLeaguesQueryHandler(Context, CurrentUserService);
        var query = new GetUserLeaguesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(1);

        var leagueSummary = result.Value[0];
        await Assert.That(leagueSummary.Id).IsEqualTo(league.Id);
        await Assert.That(leagueSummary.Name).IsEqualTo("Test League");
        await Assert.That(leagueSummary.OwnerUsername).IsEqualTo("leagueowner");
        await Assert.That(leagueSummary.MemberCount).IsEqualTo(3);
        await Assert.That(leagueSummary.IsPublic).IsTrue();
    }

    [Test]
    public async Task GetUserLeagues_UserNotMemberOfLeague_DoesNotReturnThatLeague()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();

        Context.Users.AddRange(user, otherUser);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithOwnerId(otherUserId)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetUserLeaguesQueryHandler(Context, CurrentUserService);
        var query = new GetUserLeaguesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }
}
