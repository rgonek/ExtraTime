using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Commands.DeleteBet;
using ExtraTime.Application.Features.Bets.Commands.PlaceBet;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.NewIntegrationTests.Base;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.NewIntegrationTests.Tests.Bets;

public sealed class BetLifecycleTests : NewIntegrationTestBase
{
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);
    private FakeClock _fakeClock = null!;

    [Before(Test)]
    public void SetupClock()
    {
        _fakeClock = new FakeClock(_now);
        Clock.Current = _fakeClock;
    }

    [After(Test)]
    public void CleanupClock()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task PlaceBet_ValidData_CreatesBetInDatabase()
    {
        Clock.Current = new FakeClock(_now);
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        
        var competition = new CompetitionBuilder().Build();
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        
        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();

        Context.Users.Add(user);
        Context.Competitions.Add(competition);
        Context.Teams.AddRange(homeTeam, awayTeam);
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(_now.AddHours(2))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new PlaceBetCommandHandler(Context, CurrentUserService);
        var command = new PlaceBetCommand(league.Id, match.Id, 2, 1);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.PredictedHomeScore).IsEqualTo(2);
        await Assert.That(result.Value.PredictedAwayScore).IsEqualTo(1);

        var bet = await Context.Bets
            .FirstOrDefaultAsync(b => b.LeagueId == league.Id && b.UserId == userId && b.MatchId == match.Id);

        await Assert.That(bet).IsNotNull();
        await Assert.That(bet!.PredictedHomeScore).IsEqualTo(2);
        await Assert.That(bet.PredictedAwayScore).IsEqualTo(1);
    }

    [Test]
    public async Task PlaceBet_NotMember_ReturnsFailure()
    {
        Clock.Current = new FakeClock(_now);
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var nonMemberId = Guid.NewGuid();
        var nonMember = new UserBuilder().WithId(nonMemberId).Build();
        Context.Users.Add(nonMember);

        var competition = new CompetitionBuilder().Build();
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
        Context.Teams.AddRange(homeTeam, awayTeam);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(_now.AddHours(2))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        SetCurrentUser(nonMemberId);

        var handler = new PlaceBetCommandHandler(Context, CurrentUserService);
        var command = new PlaceBetCommand(league.Id, match.Id, 2, 1);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task PlaceBet_DeadlinePassed_ReturnsFailure()
    {
        Clock.Current = new FakeClock(_now);
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var competition = new CompetitionBuilder().Build();
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
        Context.Teams.AddRange(homeTeam, awayTeam);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithBettingDeadlineMinutes(60) // 60 minutes before match
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        // Match starts in 30 minutes, deadline is 60 minutes before = 30 minutes ago
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(_now.AddMinutes(30))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new PlaceBetCommandHandler(Context, CurrentUserService);
        var command = new PlaceBetCommand(league.Id, match.Id, 2, 1);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task PlaceBet_ExistingBet_UpdatesBet()
    {
        Clock.Current = new FakeClock(_now);
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        
        var competition = new CompetitionBuilder().Build();
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        
        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();

        Context.Users.Add(user);
        Context.Competitions.Add(competition);
        Context.Teams.AddRange(homeTeam, awayTeam);
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(_now.AddHours(2))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        // Create initial bet
        var existingBet = Bet.Place(league.Id, userId, match.Id, 1, 0);
        Context.Bets.Add(existingBet);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new PlaceBetCommandHandler(Context, CurrentUserService);
        var command = new PlaceBetCommand(league.Id, match.Id, 2, 1); // Different score

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.PredictedHomeScore).IsEqualTo(2);
        await Assert.That(result.Value.PredictedAwayScore).IsEqualTo(1);

        var updatedBet = await Context.Bets.FindAsync(existingBet.Id);
        await Assert.That(updatedBet!.PredictedHomeScore).IsEqualTo(2);
        await Assert.That(updatedBet.PredictedAwayScore).IsEqualTo(1);
    }

    //
    // Delete Bet Tests
    //

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
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
        Context.Teams.AddRange(homeTeam, awayTeam);

        // Match is scheduled for tomorrow
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
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
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
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
        Context.Teams.AddRange(homeTeam, awayTeam);

        // Match starts in 1 hour
        var matchStartTime = _now.AddHours(1);
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

        // Advance time past the deadline 
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
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
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
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
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

        // Assert 
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(BetErrors.BetNotFound);
    }
}
