using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Commands.DeleteBet;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

[TestCategory(TestCategories.Significant)]
public sealed class DeleteBetCommandIntegrationTests : IntegrationTestBase
{
    private FakeClock _fakeClock = null!;

    [Before(Test)]
    public new async Task SetupAsync()
    {
        await base.SetupAsync();
        _fakeClock = new FakeClock(DateTime.UtcNow);
        Clock.Current = _fakeClock;
    }

    [After(Test)]
    public new async ValueTask TeardownAsync()
    {
        Clock.Current = null!;
        await base.TeardownAsync();
    }

    [Test]
    public async Task DeleteBet_ValidScheduledMatch_DeletesBet()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithBettingDeadlineMinutes(5)
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        // Match is scheduled for tomorrow (plenty of time before deadline)
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
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new DeleteBetCommandHandler(Context, CurrentUserService);
        var command = new DeleteBetCommand(league.Id, bet.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var deletedBet = await Context.Bets
            .FirstOrDefaultAsync(b => b.Id == bet.Id);
        await Assert.That(deletedBet).IsNull();
    }

    [Test]
    public async Task DeleteBet_MatchAlreadyStarted_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithBettingDeadlineMinutes(5)
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
            .WithMatchDate(Clock.UtcNow.AddDays(1))
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();
        Context.Matches.Add(match);

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new DeleteBetCommandHandler(Context, CurrentUserService);
        var command = new DeleteBetCommand(league.Id, bet.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.MatchAlreadyStarted);
    }

    [Test]
    [TestCategory(TestCategories.RequiresDatabase)]
    public async Task DeleteBet_DeadlinePassed_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithBettingDeadlineMinutes(5)
            .Build();
        Context.Leagues.Add(league);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        // Match starts in 1 hour, deadline is 5 minutes before (55 minutes from now)
        var matchStartTime = Clock.UtcNow.AddHours(1);
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(matchStartTime)
            .WithStatus(MatchStatus.Timed)
            .Build();
        Context.Matches.Add(match);

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        // Advance time past the deadline (need to advance by 56+ minutes to pass the 55-minute deadline)
        _fakeClock.AdvanceBy(TimeSpan.FromMinutes(56));

        SetCurrentUser(userId);

        var handler = new DeleteBetCommandHandler(Context, CurrentUserService);
        var command = new DeleteBetCommand(league.Id, bet.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.DeadlinePassed);
    }

    [Test]
    public async Task DeleteBet_BetNotFound_ReturnsFailure()
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

        var handler = new DeleteBetCommandHandler(Context, CurrentUserService);
        var command = new DeleteBetCommand(league.Id, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.BetNotFound);
    }

    [Test]
    public async Task DeleteBet_NotBetOwner_ReturnsFailure()
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

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(Clock.UtcNow.AddDays(1))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);

        // Bet is owned by ownerId
        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(ownerId)
            .WithMatchId(match.Id)
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        // Try to delete as otherUser
        SetCurrentUser(otherUserId);

        var handler = new DeleteBetCommandHandler(Context, CurrentUserService);
        var command = new DeleteBetCommand(league.Id, bet.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.NotBetOwner);
    }

    [Test]
    public async Task DeleteBet_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(Clock.UtcNow.AddDays(1))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        // Create bet in existing league
        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new DeleteBetCommandHandler(Context, CurrentUserService);
        // Try to delete using non-existent league ID
        var command = new DeleteBetCommand(Guid.NewGuid(), bet.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert - Handler filters by both leagueId and betId, so bet won't be found
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.BetNotFound);
    }
}
