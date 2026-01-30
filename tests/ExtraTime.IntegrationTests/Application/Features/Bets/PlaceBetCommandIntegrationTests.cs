using ExtraTime.Application.Features.Bets.Commands.PlaceBet;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

public sealed class PlaceBetCommandIntegrationTests : IntegrationTestBase
{
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    [Before(Test)]
    public void Setup()
    {
        Clock.Current = new FakeClock(_now);
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task PlaceBet_ValidData_CreatesBetInDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
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
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var nonMemberId = Guid.NewGuid();
        var nonMember = new UserBuilder().WithId(nonMemberId).Build();
        Context.Users.Add(nonMember);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
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
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithBettingDeadlineMinutes(60) // 60 minutes before match
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        // Match starts in 30 minutes, deadline is 60 minutes before = 30 minutes ago
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
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
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
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
}
