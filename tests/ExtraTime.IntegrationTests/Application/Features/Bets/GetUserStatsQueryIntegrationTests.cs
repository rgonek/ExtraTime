using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Queries.GetUserStats;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

public sealed class GetUserStatsQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetUserStats_ExistingMember_ReturnsStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder()
            .WithId(userId)
            .WithUsername("testuser")
            .Build();

        var targetUserId = Guid.NewGuid();
        var targetUser = new UserBuilder()
            .WithId(targetUserId)
            .WithUsername("targetuser")
            .Build();

        Context.Users.AddRange(user, targetUser);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        // Add both users as members
        var userMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Owner)
            .Build();
        var targetMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(targetUserId)
            .WithRole(MemberRole.Member)
            .Build();
        Context.LeagueMembers.AddRange(userMembership, targetMembership);

        // Create standing for target user
        var standing = LeagueStanding.Create(league.Id, targetUserId);
        standing.ApplyBetResult(5, true, true);  // Exact match
        standing.ApplyBetResult(3, false, true); // Correct result
        standing.ApplyBetResult(0, false, false); // Wrong
        Context.LeagueStandings.Add(standing);

        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
        var query = new GetUserStatsQuery(league.Id, targetUserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.UserId).IsEqualTo(targetUserId);
        await Assert.That(result.Value.Username).IsEqualTo("targetuser");
        await Assert.That(result.Value.TotalPoints).IsEqualTo(8); // 5 + 3
        await Assert.That(result.Value.BetsPlaced).IsEqualTo(3);
        await Assert.That(result.Value.ExactMatches).IsEqualTo(1);
        await Assert.That(result.Value.CorrectResults).IsEqualTo(2);
        await Assert.That(result.Value.AccuracyPercentage).IsEqualTo(66.67);
    }

    [Test]
    public async Task GetUserStats_NotAMember_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();

        var targetUserId = Guid.NewGuid();
        var targetUser = new UserBuilder().WithId(targetUserId).Build();

        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();

        Context.Users.AddRange(user, targetUser, owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);

        // Only owner and target user are members, user is not
        var ownerMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(ownerId)
            .WithRole(MemberRole.Owner)
            .Build();
        var targetMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(targetUserId)
            .WithRole(MemberRole.Member)
            .Build();
        Context.LeagueMembers.AddRange(ownerMembership, targetMembership);
        await Context.SaveChangesAsync();

        // Set user as current user (who is not a member)
        SetCurrentUser(userId);

        var handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
        var query = new GetUserStatsQuery(league.Id, targetUserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.NotALeagueMember);
    }

    [Test]
    public async Task GetUserStats_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();

        var nonExistentUserId = Guid.NewGuid();

        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        // Only user is a member
        var userMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Owner)
            .Build();
        Context.LeagueMembers.Add(userMembership);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
        var query = new GetUserStatsQuery(league.Id, nonExistentUserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.UserNotFound);
    }

    [Test]
    public async Task GetUserStats_WithStandings_ReturnsCorrectRank()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder()
            .WithId(userId)
            .WithUsername("testuser")
            .Build();

        var targetUserId = Guid.NewGuid();
        var targetUser = new UserBuilder()
            .WithId(targetUserId)
            .WithUsername("targetuser")
            .Build();

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder()
            .WithId(otherUserId)
            .WithUsername("otheruser")
            .Build();

        Context.Users.AddRange(user, targetUser, otherUser);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        // Add all users as members
        var userMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Owner)
            .Build();
        var targetMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(targetUserId)
            .WithRole(MemberRole.Member)
            .Build();
        var otherMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(otherUserId)
            .WithRole(MemberRole.Member)
            .Build();
        Context.LeagueMembers.AddRange(userMembership, targetMembership, otherMembership);

        // Create standings - target user has 10 points, other user has 5 points
        var userStanding = LeagueStanding.Create(league.Id, userId);
        userStanding.ApplyBetResult(5, true, true);
        userStanding.ApplyBetResult(3, false, true);
        Context.LeagueStandings.Add(userStanding);

        var targetStanding = LeagueStanding.Create(league.Id, targetUserId);
        targetStanding.ApplyBetResult(5, true, true); // 5 points
        targetStanding.ApplyBetResult(5, true, true); // 5 points
        Context.LeagueStandings.Add(targetStanding);

        var otherStanding = LeagueStanding.Create(league.Id, otherUserId);
        otherStanding.ApplyBetResult(3, false, true);
        otherStanding.ApplyBetResult(2, false, true);
        Context.LeagueStandings.Add(otherStanding);

        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
        var query = new GetUserStatsQuery(league.Id, targetUserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert - Target user has 10 points, should be rank 1 (tied with user who also has 8 points)
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.TotalPoints).IsEqualTo(10);
        await Assert.That(result.Value.Rank).IsEqualTo(1);
    }
}
