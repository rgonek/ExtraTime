using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Queries.GetLeagueStandings;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

[TestCategory(TestCategories.Significant)]
public sealed class GetLeagueStandingsQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetLeagueStandings_UserIsMember_ReturnsOrderedStandings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder()
            .WithId(userId)
            .WithUsername("testuser")
            .WithEmail("test@example.com")
            .Build();

        var member2Id = Guid.NewGuid();
        var member2 = new UserBuilder()
            .WithId(member2Id)
            .WithUsername("member2")
            .WithEmail("member2@example.com")
            .Build();

        Context.Users.AddRange(user, member2);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        // Add member2 to league
        var member2Membership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(member2Id)
            .WithRole(MemberRole.Member)
            .Build();
        Context.LeagueMembers.Add(member2Membership);

        // Create standings for both members
        var userStanding = LeagueStanding.Create(league.Id, userId);
        userStanding.ApplyBetResult(5, true, true);
        userStanding.ApplyBetResult(3, true, false);
        Context.LeagueStandings.Add(userStanding);

        var member2Standing = LeagueStanding.Create(league.Id, member2Id);
        member2Standing.ApplyBetResult(3, false, true);
        Context.LeagueStandings.Add(member2Standing);

        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(2);

        // Rank 1: user has 8 total points (5 + 3), 2 exact matches
        var first = result.Value[0];
        await Assert.That(first.Rank).IsEqualTo(1);
        await Assert.That(first.UserId).IsEqualTo(userId);
        await Assert.That(first.Username).IsEqualTo("testuser");
        await Assert.That(first.Email).IsEqualTo("test@example.com");
        await Assert.That(first.TotalPoints).IsEqualTo(8);
        await Assert.That(first.BetsPlaced).IsEqualTo(2);
        await Assert.That(first.ExactMatches).IsEqualTo(2);

        // Rank 2: member2 has 3 total points, 0 exact matches
        var second = result.Value[1];
        await Assert.That(second.Rank).IsEqualTo(2);
        await Assert.That(second.UserId).IsEqualTo(member2Id);
        await Assert.That(second.TotalPoints).IsEqualTo(3);
        await Assert.That(second.BetsPlaced).IsEqualTo(1);
        await Assert.That(second.ExactMatches).IsEqualTo(0);
    }

    [Test]
    public async Task GetLeagueStandings_UserIsNotMember_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();

        Context.Users.AddRange(owner, otherUser);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);

        await Context.SaveChangesAsync();

        SetCurrentUser(otherUserId);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.NotALeagueMember);
    }

    [Test]
    public async Task GetLeagueStandings_NoStandings_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetLeagueStandings_ExcludesKickedMembers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("activeuser").Build();

        var kickedUserId = Guid.NewGuid();
        var kickedUser = new UserBuilder().WithId(kickedUserId).WithUsername("kickeduser").Build();

        Context.Users.AddRange(user, kickedUser);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        // Create standings for both users
        var userStanding = LeagueStanding.Create(league.Id, userId);
        userStanding.ApplyBetResult(3, true, false);
        Context.LeagueStandings.Add(userStanding);

        var kickedStanding = LeagueStanding.Create(league.Id, kickedUserId);
        kickedStanding.ApplyBetResult(1, false, true);
        Context.LeagueStandings.Add(kickedStanding);

        await Context.SaveChangesAsync();

        // Only user is a current member - kicked user has no LeagueMember record
        // (simulating that they were removed from the league)

        SetCurrentUser(userId);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        await Assert.That(result.Value[0].UserId).IsEqualTo(userId);
        await Assert.That(result.Value[0].Username).IsEqualTo("activeuser");
    }

    [Test]
    public async Task GetLeagueStandings_TieBreaker_ByExactMatches()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user1 = new UserBuilder().WithId(user1Id).WithUsername("user1").Build();

        var user2Id = Guid.NewGuid();
        var user2 = new UserBuilder().WithId(user2Id).WithUsername("user2").Build();

        Context.Users.AddRange(user1, user2);

        var league = new LeagueBuilder()
            .WithOwnerId(user1Id)
            .Build();
        Context.Leagues.Add(league);

        // Add user2 as member
        var user2Membership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(user2Id)
            .WithRole(MemberRole.Member)
            .Build();
        Context.LeagueMembers.Add(user2Membership);

        // Same total points, different exact matches
        var user1Standing = LeagueStanding.Create(league.Id, user1Id);
        user1Standing.ApplyBetResult(3, false, true);
        user1Standing.ApplyBetResult(3, false, true);
        user1Standing.ApplyBetResult(1, false, true);
        Context.LeagueStandings.Add(user1Standing);

        var user2Standing = LeagueStanding.Create(league.Id, user2Id);
        user2Standing.ApplyBetResult(3, true, false); // 1 exact match
        user2Standing.ApplyBetResult(3, false, true);
        user2Standing.ApplyBetResult(1, false, true);
        Context.LeagueStandings.Add(user2Standing);

        await Context.SaveChangesAsync();

        SetCurrentUser(user1Id);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert - Both have 7 points, but user2 has 1 exact match vs user1's 0
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
        
        // user2 should rank higher due to more exact matches
        await Assert.That(result.Value[0].UserId).IsEqualTo(user2Id);
        await Assert.That(result.Value[0].TotalPoints).IsEqualTo(7);
        await Assert.That(result.Value[0].ExactMatches).IsEqualTo(1);
        
        await Assert.That(result.Value[1].UserId).IsEqualTo(user1Id);
        await Assert.That(result.Value[1].TotalPoints).IsEqualTo(7);
        await Assert.That(result.Value[1].ExactMatches).IsEqualTo(0);
    }

    [Test]
    public async Task GetLeagueStandings_TieBreaker_ByBetsPlaced()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user1 = new UserBuilder().WithId(user1Id).WithUsername("user1").Build();

        var user2Id = Guid.NewGuid();
        var user2 = new UserBuilder().WithId(user2Id).WithUsername("user2").Build();

        Context.Users.AddRange(user1, user2);

        var league = new LeagueBuilder()
            .WithOwnerId(user1Id)
            .Build();
        Context.Leagues.Add(league);

        // Add user2 as member
        var user2Membership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(user2Id)
            .WithRole(MemberRole.Member)
            .Build();
        Context.LeagueMembers.Add(user2Membership);

        // Same total points, same exact matches, different bets placed
        var user1Standing = LeagueStanding.Create(league.Id, user1Id);
        user1Standing.ApplyBetResult(3, true, false);
        user1Standing.ApplyBetResult(3, true, false);
        Context.LeagueStandings.Add(user1Standing);

        var user2Standing = LeagueStanding.Create(league.Id, user2Id);
        user2Standing.ApplyBetResult(3, true, false);
        user2Standing.ApplyBetResult(3, true, false);
        user2Standing.ApplyBetResult(0, false, false); // Lost bet - 0 points
        Context.LeagueStandings.Add(user2Standing);

        await Context.SaveChangesAsync();

        SetCurrentUser(user1Id);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert - Both have 6 points, 2 exact matches, but user1 has fewer bets
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
        
        // user1 should rank higher due to fewer bets placed
        await Assert.That(result.Value[0].UserId).IsEqualTo(user1Id);
        await Assert.That(result.Value[0].TotalPoints).IsEqualTo(6);
        await Assert.That(result.Value[0].BetsPlaced).IsEqualTo(2);
        
        await Assert.That(result.Value[1].UserId).IsEqualTo(user2Id);
        await Assert.That(result.Value[1].TotalPoints).IsEqualTo(6);
        await Assert.That(result.Value[1].BetsPlaced).IsEqualTo(3);
    }
}
