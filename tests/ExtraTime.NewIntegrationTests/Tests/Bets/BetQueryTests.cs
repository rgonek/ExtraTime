using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Queries.GetLeagueStandings;
using ExtraTime.Application.Features.Bets.Queries.GetMatchBets;
using ExtraTime.Application.Features.Bets.Queries.GetMyBets;
using ExtraTime.Application.Features.Bets.Queries.GetUserStats;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.NewIntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.NewIntegrationTests.Tests.Bets;

public sealed class BetQueryTests : NewIntegrationTestBase
{
    //
    // Get Match Bets Tests
    //

    [Test]
    public async Task GetMatchBets_MemberAfterDeadline_ReturnsBets()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBettingDeadlineMinutes(60)
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var matchStartTime = DateTime.UtcNow.AddHours(-2);
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(matchStartTime)
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();
        Context.Matches.Add(match);

        var bettor1 = new UserBuilder().WithUsername("Bettor1").Build();
        var bettor2 = new UserBuilder().WithUsername("Bettor2").Build();
        Context.Users.AddRange(bettor1, bettor2);
        await Context.SaveChangesAsync();

        // Add members to league
        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(bettor1.Id, MemberRole.Member);
        trackedLeague.AddMember(bettor2.Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        var bet1 = Bet.Place(league.Id, bettor1.Id, match.Id, 2, 1);
        var bet2 = Bet.Place(league.Id, bettor2.Id, match.Id, 1, 0);
        Context.Bets.AddRange(bet1, bet2);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMatchBetsQuery(league.Id, match.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetMatchBets_MemberBeforeDeadline_ReturnsEmptyList()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBettingDeadlineMinutes(60)
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var matchStartTime = DateTime.UtcNow.AddHours(2);
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(matchStartTime)
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);

        var bettor1 = new UserBuilder().WithUsername("Bettor1").Build();
        Context.Users.Add(bettor1);
        await Context.SaveChangesAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(bettor1.Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        var bet1 = Bet.Place(league.Id, bettor1.Id, match.Id, 2, 1);
        Context.Bets.Add(bet1);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(ownerId);

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMatchBetsQuery(league.Id, match.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value.Count).IsEqualTo(0);
    }

    //
    // Get My Bets Tests
    //

    [Test]
    public async Task GetMyBets_UserIsMember_ReturnsBetsList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("testuser").Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().WithName("Home Team").Build();
        var awayTeam = new TeamBuilder().WithName("Away Team").Build();
        Context.Teams.AddRange(homeTeam, awayTeam);
        await Context.SaveChangesAsync();

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(Clock.UtcNow.AddDays(1))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1)
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(userId);

        var handler = new GetMyBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMyBetsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        
        var myBet = result.Value[0];
        await Assert.That(myBet.BetId).IsEqualTo(bet.Id);
        await Assert.That(myBet.MatchId).IsEqualTo(match.Id);
    }

    //
    // Get User Stats Tests
    //

    [Test]
    public async Task GetUserStats_ExistingMember_ReturnsStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("testuser").Build();

        var targetUserId = Guid.NewGuid();
        var targetUser = new UserBuilder().WithId(targetUserId).WithUsername("targetuser").Build();

        Context.Users.AddRange(user, targetUser);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(targetUserId, MemberRole.Member);
        await Context.SaveChangesAsync();
        
        // Create standing for target user
        var standing = LeagueStanding.Create(league.Id, targetUserId);
        standing.ApplyBetResult(5, true, true);  // Exact match
        standing.ApplyBetResult(3, false, true); // Correct result
        standing.ApplyBetResult(0, false, false); // Wrong
        Context.LeagueStandings.Add(standing);

        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(userId);

        var handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
        var query = new GetUserStatsQuery(league.Id, targetUserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.UserId).IsEqualTo(targetUserId);
        await Assert.That(result.Value.TotalPoints).IsEqualTo(8);
        await Assert.That(result.Value.BetsPlaced).IsEqualTo(3);
        await Assert.That(result.Value.ExactMatches).IsEqualTo(1);
        await Assert.That(result.Value.CorrectResults).IsEqualTo(2);
    }

    //
    // Get League Standings Tests
    //

    [Test]
    public async Task GetLeagueStandings_UserIsMember_ReturnsOrderedStandings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("testuser").Build();

        var member2Id = Guid.NewGuid();
        var member2 = new UserBuilder().WithId(member2Id).WithUsername("member2").Build();

        Context.Users.AddRange(user, member2);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(member2Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        var userStanding = LeagueStanding.Create(league.Id, userId);
        userStanding.ApplyBetResult(5, true, true);
        userStanding.ApplyBetResult(3, true, false); 
        Context.LeagueStandings.Add(userStanding);

        var member2Standing = LeagueStanding.Create(league.Id, member2Id);
        member2Standing.ApplyBetResult(3, false, true);
        Context.LeagueStandings.Add(member2Standing);

        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        SetCurrentUser(userId);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);

        var first = result.Value[0];
        await Assert.That(first.Rank).IsEqualTo(1);
        await Assert.That(first.UserId).IsEqualTo(userId);
        await Assert.That(first.TotalPoints).IsEqualTo(8);

        var second = result.Value[1];
        await Assert.That(second.Rank).IsEqualTo(2);
        await Assert.That(second.UserId).IsEqualTo(member2Id);
        await Assert.That(second.TotalPoints).IsEqualTo(3);
    }
}
