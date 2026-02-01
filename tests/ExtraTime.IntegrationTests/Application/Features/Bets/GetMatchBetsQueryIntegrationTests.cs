using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Queries.GetMatchBets;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

[TestCategory(TestCategories.Significant)]
public sealed class GetMatchBetsQueryIntegrationTests : IntegrationTestBase
{
    private readonly DateTime _matchDate = DateTime.UtcNow.AddDays(1);

    [Test]
    public async Task GetMatchBets_MemberAfterDeadline_ReturnsBets()
    {
        // Arrange - Set up match that already started (deadline passed)
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBettingDeadlineMinutes(60) // 1 hour deadline
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.Add(homeTeam);
        Context.Teams.Add(awayTeam);

        // Match that started 2 hours ago (deadline definitely passed)
        var matchStartTime = DateTime.UtcNow.AddHours(-2);
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(matchStartTime)
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();
        Context.Matches.Add(match);

        // Create bettors
        var bettor1 = new UserBuilder().WithUsername("Bettor1").Build();
        var bettor2 = new UserBuilder().WithUsername("Bettor2").Build();
        Context.Users.Add(bettor1);
        Context.Users.Add(bettor2);

        // Add members to league
        league.AddMember(bettor1.Id, MemberRole.Member);
        league.AddMember(bettor2.Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        // Create bets
        var bet1 = Bet.Place(league.Id, bettor1.Id, match.Id, 2, 1);
        var bet2 = Bet.Place(league.Id, bettor2.Id, match.Id, 1, 0);
        Context.Bets.Add(bet1);
        Context.Bets.Add(bet2);
        await Context.SaveChangesAsync();

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
        // Arrange - Set up match in future (deadline not passed)
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBettingDeadlineMinutes(60) // 1 hour deadline
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.Add(homeTeam);
        Context.Teams.Add(awayTeam);

        // Match 2 hours in the future (deadline not passed yet)
        var matchStartTime = DateTime.UtcNow.AddHours(2);
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(matchStartTime)
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);

        // Create bettor
        var bettor1 = new UserBuilder().WithUsername("Bettor1").Build();
        Context.Users.Add(bettor1);
        league.AddMember(bettor1.Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        // Create bet
        var bet1 = Bet.Place(league.Id, bettor1.Id, match.Id, 2, 1);
        Context.Bets.Add(bet1);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMatchBetsQuery(league.Id, match.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert - Bets hidden before deadline
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
        Context.Teams.Add(homeTeam);
        Context.Teams.Add(awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(DateTime.UtcNow.AddHours(-2))
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        SetCurrentUser(otherUserId); // Not a member of this league

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMatchBetsQuery(league.Id, match.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert - Handler checks membership first, returns NotALeagueMember
        await Assert.That(result.IsSuccess).IsFalse();
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

        SetCurrentUser(userId);

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        // Query with non-existent league ID - user is NOT a member of THIS league
        var query = new GetMatchBetsQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert - Handler checks membership first. Since user is not a member of the
        // requested league (non-existent), it returns NotALeagueMember.
        // This is the correct security behavior - don't leak info about league existence.
        await Assert.That(result.IsSuccess).IsFalse();
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

        SetCurrentUser(ownerId);

        var handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
        var query = new GetMatchBetsQuery(league.Id, Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("Match not found");
    }

    [Test]
    public async Task GetMatchBets_NoBets_ReturnsEmptyList()
    {
        // Arrange - Set up match that started but no bets placed
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
        Context.Teams.Add(homeTeam);
        Context.Teams.Add(awayTeam);

        // Match that started 2 hours ago
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
        Context.Teams.Add(homeTeam);
        Context.Teams.Add(awayTeam);

        // Match that started 2 hours ago with final score 2-1
        var matchStartTime = DateTime.UtcNow.AddHours(-2);
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(matchStartTime)
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();
        Context.Matches.Add(match);

        // Create bettor with exact match prediction
        var bettor1 = new UserBuilder().WithUsername("Bettor1").Build();
        Context.Users.Add(bettor1);
        league.AddMember(bettor1.Id, MemberRole.Member);
        await Context.SaveChangesAsync();

        // Create bet with exact match prediction
        var bet1 = Bet.Place(league.Id, bettor1.Id, match.Id, 2, 1);
        Context.Bets.Add(bet1);

        // Calculate and add result
        var result1 = bet1.CalculatePoints(match, 5, 2);
        var betResult1 = new BetResultBuilder()
            .WithBetId(bet1.Id)
            .WithPointsEarned(result1.PointsEarned)
            .WithIsExactMatch(result1.IsExactMatch)
            .WithIsCorrectResult(result1.IsCorrectResult)
            .Build();
        Context.BetResults.Add(betResult1);
        await Context.SaveChangesAsync();

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
        await Assert.That(result.Value[0].Result!.PointsEarned).IsEqualTo(5); // Exact match = 5 points
        await Assert.That(result.Value[0].Result.IsExactMatch).IsTrue();
    }
}
