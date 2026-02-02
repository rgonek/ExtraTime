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

        // Assert - Before deadline, bets should not be visible (returns 0)
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetMatchBets_NotAMember_ReturnsFailure()
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

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(DateTime.UtcNow.AddHours(-2))
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(otherUserId); // Not a member of this league

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMatchBetsQuery(league.Id, match.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(BetErrors.NotALeagueMember);
    }

    [Test]
    public async Task GetMatchBets_LeagueNotFound_ReturnsNotAMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        // Create a league and add user as member
        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        // Query with non-existent league ID
        var query = new GetMatchBetsQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(BetErrors.NotALeagueMember);
    }

    [Test]
    public async Task GetMatchBets_MatchNotFound_ReturnsFailure()
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

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMatchBetsQuery(league.Id, Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(BetErrors.MatchNotFound);
    }

    [Test]
    public async Task GetMatchBets_NoBets_ReturnsEmptyList()
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

    [Test]
    public async Task GetMatchBets_WithResults_IncludesResultsInResponse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBettingDeadlineMinutes(60)
            .WithScoringRules(5, 2)
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
        Context.Users.Add(bettor1);
        await Context.SaveChangesAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(bettor1.Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        var bet1 = Bet.Place(league.Id, bettor1.Id, match.Id, 2, 1);
        Context.Bets.Add(bet1);

        var betResult1 = BetResult.Create(bet1.Id, 5, true, true);
        Context.BetResults.Add(betResult1);
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
        await Assert.That(result.Value.Count).IsEqualTo(1);
        await Assert.That(result.Value[0].Result).IsNotNull();
        await Assert.That(result.Value[0].Result!.PointsEarned).IsEqualTo(5);
        await Assert.That(result.Value[0].Result!.IsExactMatch).IsTrue();
    }

    //
    // Get My Bets Tests
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

    [Test]
    public async Task GetMyBets_UserIsNotMember_ReturnsFailure()
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

        Context.ChangeTracker.Clear();
        SetCurrentUser(otherUserId);

        var handler = new GetMyBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMyBetsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(BetErrors.NotALeagueMember);
    }

    [Test]
    public async Task GetMyBets_NoBets_ReturnsEmptyList()
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

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetMyBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMyBetsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetMyBets_MultipleBets_ReturnsOrderedByMatchDateDesc()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        // Create matches with different dates
        var match1 = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(DateTime.UtcNow.AddDays(1))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match1);

        var match2 = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(DateTime.UtcNow.AddDays(2))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match2);

        var match3 = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(DateTime.UtcNow.AddHours(6))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match3);

        await Context.SaveChangesAsync();

        var bet1 = Bet.Place(league.Id, userId, match1.Id, 1, 1);
        var bet2 = Bet.Place(league.Id, userId, match2.Id, 2, 2);
        var bet3 = Bet.Place(league.Id, userId, match3.Id, 3, 3);
        Context.Bets.AddRange(bet1, bet2, bet3);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetMyBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMyBetsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(3);
        
        // Should be ordered by MatchDateUtc descending (latest first)
        await Assert.That(result.Value[0].MatchId).IsEqualTo(match2.Id); // +2 days
        await Assert.That(result.Value[1].MatchId).IsEqualTo(match1.Id); // +1 day
        await Assert.That(result.Value[2].MatchId).IsEqualTo(match3.Id); // +6 hours
    }

    [Test]
    public async Task GetMyBets_BetWithResult_IncludesResultDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(DateTime.UtcNow.AddDays(-1))
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        var bet = Bet.Place(league.Id, userId, match.Id, 2, 1);
        Context.Bets.Add(bet);

        var betResult = BetResult.Create(bet.Id, 3, true, true);
        Context.BetResults.Add(betResult);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetMyBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMyBetsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        
        var myBet = result.Value[0];
        await Assert.That(myBet.Result).IsNotNull();
        await Assert.That(myBet.Result!.PointsEarned).IsEqualTo(3);
        await Assert.That(myBet.Result.IsExactMatch).IsTrue();
    }

    [Test]
    public async Task GetMyBets_OnlyReturnsCurrentUserBets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();

        Context.Users.AddRange(user, otherUser);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(otherUserId, MemberRole.Member);
        await Context.SaveChangesAsync();

        var userBet = Bet.Place(league.Id, userId, match.Id, 1, 1);
        var otherBet = Bet.Place(league.Id, otherUserId, match.Id, 2, 2);
        Context.Bets.AddRange(userBet, otherBet);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetMyBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMyBetsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        await Assert.That(result.Value[0].BetId).IsEqualTo(userBet.Id);
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
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId); // Not a member

        var handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
        var query = new GetUserStatsQuery(league.Id, targetUserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
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
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
        var query = new GetUserStatsQuery(league.Id, nonExistentUserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(BetErrors.UserNotFound);
    }

    [Test]
    public async Task GetUserStats_WithStandings_ReturnsCorrectRank()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();

        var targetUserId = Guid.NewGuid();
        var targetUser = new UserBuilder().WithId(targetUserId).Build();

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();

        Context.Users.AddRange(user, targetUser, otherUser);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(targetUserId, MemberRole.Member);
        trackedLeague.AddMember(otherUserId, MemberRole.Member);
        await Context.SaveChangesAsync();

        var targetStanding = LeagueStanding.Create(league.Id, targetUserId);
        targetStanding.ApplyBetResult(10, true, true);
        Context.LeagueStandings.Add(targetStanding);

        var userStanding = LeagueStanding.Create(league.Id, userId);
        userStanding.ApplyBetResult(8, true, true);
        Context.LeagueStandings.Add(userStanding);

        var otherStanding = LeagueStanding.Create(league.Id, otherUserId);
        otherStanding.ApplyBetResult(5, false, true);
        Context.LeagueStandings.Add(otherStanding);

        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
        var query = new GetUserStatsQuery(league.Id, targetUserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.TotalPoints).IsEqualTo(10);
        await Assert.That(result.Value.Rank).IsEqualTo(1);
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

        Context.ChangeTracker.Clear();
        SetCurrentUser(otherUserId);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
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

        Context.ChangeTracker.Clear();
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
        var user = new UserBuilder().WithId(userId).Build();

        var kickedUserId = Guid.NewGuid();
        var kickedUser = new UserBuilder().WithId(kickedUserId).Build();

        Context.Users.AddRange(user, kickedUser);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        // Standing for both, but only one is member
        var userStanding = LeagueStanding.Create(league.Id, userId);
        userStanding.ApplyBetResult(3, true, false);
        Context.LeagueStandings.Add(userStanding);

        var kickedStanding = LeagueStanding.Create(league.Id, kickedUserId);
        kickedStanding.ApplyBetResult(1, false, true);
        Context.LeagueStandings.Add(kickedStanding);

        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(userId);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        await Assert.That(result.Value[0].UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task GetLeagueStandings_TieBreaker_ByExactMatches()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user1 = new UserBuilder().WithId(user1Id).Build();

        var user2Id = Guid.NewGuid();
        var user2 = new UserBuilder().WithId(user2Id).Build();

        Context.Users.AddRange(user1, user2);

        var league = new LeagueBuilder()
            .WithOwnerId(user1Id)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(user2Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        var user1Standing = LeagueStanding.Create(league.Id, user1Id);
        user1Standing.ApplyBetResult(6, false, true);
        Context.LeagueStandings.Add(user1Standing);

        var user2Standing = LeagueStanding.Create(league.Id, user2Id);
        user2Standing.ApplyBetResult(6, true, false); // 6 points with exact match
        Context.LeagueStandings.Add(user2Standing);

        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(user1Id);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert - user2 should be higher rank
        await Assert.That(result.Value[0].UserId).IsEqualTo(user2Id);
        await Assert.That(result.Value[1].UserId).IsEqualTo(user1Id);
    }

    [Test]
    public async Task GetLeagueStandings_TieBreaker_ByBetsPlaced()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user1 = new UserBuilder().WithId(user1Id).Build();

        var user2Id = Guid.NewGuid();
        var user2 = new UserBuilder().WithId(user2Id).Build();

        Context.Users.AddRange(user1, user2);

        var league = new LeagueBuilder()
            .WithOwnerId(user1Id)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.Members).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddMember(user2Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        var user1Standing = LeagueStanding.Create(league.Id, user1Id);
        user1Standing.ApplyBetResult(3, true, true);
        Context.LeagueStandings.Add(user1Standing);

        var user2Standing = LeagueStanding.Create(league.Id, user2Id);
        user2Standing.ApplyBetResult(3, true, true);
        user2Standing.ApplyBetResult(0, false, false); // Extra bet with 0 points
        Context.LeagueStandings.Add(user2Standing);

        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        SetCurrentUser(user1Id);

        var handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueStandingsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert - user1 should be higher rank (fewer bets)
        await Assert.That(result.Value[0].UserId).IsEqualTo(user1Id);
        await Assert.That(result.Value[1].UserId).IsEqualTo(user2Id);
    }
}
