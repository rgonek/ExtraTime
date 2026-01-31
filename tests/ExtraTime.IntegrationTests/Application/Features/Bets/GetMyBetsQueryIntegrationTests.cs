using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Queries.GetMyBets;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

[TestCategory(TestCategories.Significant)]
public sealed class GetMyBetsQueryIntegrationTests : IntegrationTestBase
{
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

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(Clock.UtcNow.AddDays(1))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1)
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

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
        await Assert.That(myBet.HomeTeamName).IsEqualTo("Home Team");
        await Assert.That(myBet.AwayTeamName).IsEqualTo("Away Team");
        await Assert.That(myBet.PredictedHomeScore).IsEqualTo(2);
        await Assert.That(myBet.PredictedAwayScore).IsEqualTo(1);
        await Assert.That(myBet.MatchStatus).IsEqualTo(MatchStatus.Scheduled);
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

        SetCurrentUser(otherUserId);

        var handler = new GetMyBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMyBetsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
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

        var homeTeam = new TeamBuilder().WithName("Home Team").Build();
        var awayTeam = new TeamBuilder().WithName("Away Team").Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        // Create matches with different dates
        var match1 = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(Clock.UtcNow.AddDays(1))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match1);

        var match2 = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(Clock.UtcNow.AddDays(2))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match2);

        var match3 = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(Clock.UtcNow.AddHours(6))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match3);

        await Context.SaveChangesAsync();

        // Create bets for all matches
        var bet1 = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match1.Id)
            .Build();
        Context.Bets.Add(bet1);

        var bet2 = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match2.Id)
            .Build();
        Context.Bets.Add(bet2);

        var bet3 = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match3.Id)
            .Build();
        Context.Bets.Add(bet3);

        await Context.SaveChangesAsync();

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
            .WithMatchDate(Clock.UtcNow.AddDays(-1))
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();
        Context.Matches.Add(match);

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1)
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        // Add bet result
        var betResult = BetResult.Create(
            bet.Id,
            3,
            true,
            true);
        Context.BetResults.Add(betResult);
        await Context.SaveChangesAsync();

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
        await Assert.That(myBet.Result.IsCorrectResult).IsTrue();
        await Assert.That(myBet.ActualHomeScore).IsEqualTo(2);
        await Assert.That(myBet.ActualAwayScore).IsEqualTo(1);
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

        // Add other user as member
        var otherMember = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(otherUserId)
            .WithRole(MemberRole.Member)
            .Build();
        Context.LeagueMembers.Add(otherMember);

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

        // Create bet for current user
        var userBet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .Build();
        Context.Bets.Add(userBet);

        // Create bet for other user
        var otherBet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(otherUserId)
            .WithMatchId(match.Id)
            .Build();
        Context.Bets.Add(otherBet);

        await Context.SaveChangesAsync();

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
}
