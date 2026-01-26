using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class BetTests
{
    [Test]
    public async Task Place_WithValidData_ShouldInitializeBetCorrectly()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var homeScore = 2;
        var awayScore = 1;

        // Act
        var bet = Bet.Place(leagueId, userId, matchId, homeScore, awayScore);

        // Assert
        await Assert.That(bet.LeagueId).IsEqualTo(leagueId);
        await Assert.That(bet.UserId).IsEqualTo(userId);
        await Assert.That(bet.MatchId).IsEqualTo(matchId);
        await Assert.That(bet.PredictedHomeScore).IsEqualTo(homeScore);
        await Assert.That(bet.PredictedAwayScore).IsEqualTo(awayScore);
        await Assert.That(bet.DomainEvents.Any(e => e is BetPlaced)).IsTrue();
    }

    [Test]
    public async Task Update_BeforeDeadline_ShouldUpdateScores()
    {
        // Arrange
        var matchStartTime = Clock.UtcNow.AddHours(2);
        var bet = Bet.Place(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1);
        bet.ClearDomainEvents();

        // Act
        bet.Update(2, 0, Clock.UtcNow, 15, matchStartTime);

        // Assert
        await Assert.That(bet.PredictedHomeScore).IsEqualTo(2);
        await Assert.That(bet.PredictedAwayScore).IsEqualTo(0);
        await Assert.That(bet.DomainEvents.Any(e => e is BetUpdated)).IsTrue();
    }

    [Test]
    public async Task Update_AfterDeadline_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var matchStartTime = Clock.UtcNow.AddMinutes(10);
        var bet = Bet.Place(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1);

        // Act & Assert
        await Assert.That(() => bet.Update(2, 0, Clock.UtcNow, 15, matchStartTime))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task CalculatePoints_ExactMatch_ShouldReturnExactPoints()
    {
        // Arrange
        var bet = Bet.Place(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, 1);
        var match = Match.Create(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Finished);
        match.UpdateScore(2, 1);

        // Act
        var result = bet.CalculatePoints(match, 3, 1);

        // Assert
        await Assert.That(result.PointsEarned).IsEqualTo(3);
        await Assert.That(result.IsExactMatch).IsTrue();
        await Assert.That(result.IsCorrectResult).IsTrue();
    }

    [Test]
    public async Task CalculatePoints_CorrectResult_ShouldReturnResultPoints()
    {
        // Arrange
        var bet = Bet.Place(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 0); // Home win
        var match = Match.Create(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Finished);
        match.UpdateScore(3, 1); // Home win, different score

        // Act
        var result = bet.CalculatePoints(match, 3, 1);

        // Assert
        await Assert.That(result.PointsEarned).IsEqualTo(1);
        await Assert.That(result.IsExactMatch).IsFalse();
        await Assert.That(result.IsCorrectResult).IsTrue();
    }

    [Test]
    public async Task CalculatePoints_IncorrectResult_ShouldReturnZeroPoints()
    {
        // Arrange
        var bet = Bet.Place(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 0); // Home win
        var match = Match.Create(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Finished);
        match.UpdateScore(1, 2); // Away win

        // Act
        var result = bet.CalculatePoints(match, 3, 1);

        // Assert
        await Assert.That(result.PointsEarned).IsEqualTo(0);
        await Assert.That(result.IsExactMatch).IsFalse();
        await Assert.That(result.IsCorrectResult).IsFalse();
    }
}
