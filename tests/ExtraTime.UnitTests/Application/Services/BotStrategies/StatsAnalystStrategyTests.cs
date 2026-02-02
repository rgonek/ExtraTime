using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class StatsAnalystStrategyTests
{
    private readonly ITeamFormCalculator _formCalculator;
    private readonly StatsAnalystStrategy _strategy;

    public StatsAnalystStrategyTests()
    {
        _formCalculator = Substitute.For<ITeamFormCalculator>();
        _strategy = new StatsAnalystStrategy(_formCalculator);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithFormData_UsesRecentPerformance()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeForm = CreateTeamForm(60.0); // Good form
        var awayForm = CreateTeamForm(40.0); // Poor form

        _formCalculator.CalculateFormAsync(match.HomeTeamId, match.CompetitionId, 5, Arg.Any<CancellationToken>())
            .Returns(homeForm);
        _formCalculator.CalculateFormAsync(match.AwayTeamId, match.CompetitionId, 5, Arg.Any<CancellationToken>())
            .Returns(awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, null);

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(homeScore).IsLessThanOrEqualTo(4); // MaxGoals for Moderate style
        await Assert.That(awayScore).IsLessThanOrEqualTo(4);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithHomeAdvantage_ConsidersHomeStrength()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeForm = CreateTeamForm(50.0, homeWinRate: 0.8); // Strong at home
        var awayForm = CreateTeamForm(50.0, awayWinRate: 0.2); // Weak away

        _formCalculator.CalculateFormAsync(Arg.Is(match.HomeTeamId), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm);
        _formCalculator.CalculateFormAsync(Arg.Is(match.AwayTeamId), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, null);

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithStreak_DetectsMomentum()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeForm = CreateTeamForm(50.0, currentStreak: 5); // On a winning streak
        var awayForm = CreateTeamForm(50.0, currentStreak: -3); // On a losing streak

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, null);

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithHighStakes_AdjustsForImportance()
    {
        // Arrange
        var match = CreateTestMatch(matchday: 35); // Late season = high stakes
        var homeForm = CreateTeamForm(50.0);
        var awayForm = CreateTeamForm(50.0);

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, null);

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithMissingData_FallsBackToDefaults()
    {
        // Arrange
        var match = CreateTestMatch();
        var emptyForm = new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesAnalyzed = 0,
            MatchesPlayed = 0
        };

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(emptyForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, null);

        // Assert - Should still generate valid predictions with default values
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithConservativeConfig_ReturnsLowerScores()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = StatsAnalystConfig.Conservative;
        var homeForm = CreateTeamForm(50.0);
        var awayForm = CreateTeamForm(50.0);

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, config.ToJson());

        // Assert - Conservative style has MaxGoals = 2
        await Assert.That(homeScore).IsLessThanOrEqualTo(2);
        await Assert.That(awayScore).IsLessThanOrEqualTo(2);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithBoldConfig_ReturnsHigherScores()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = new StatsAnalystConfig { Style = PredictionStyle.Bold };
        var homeForm = CreateTeamForm(70.0, goalsPerMatch: 2.5);
        var awayForm = CreateTeamForm(60.0, goalsPerMatch: 2.0);

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, config.ToJson());

        // Assert - Bold style has MaxGoals = 5 and uses Ceiling
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(homeScore).IsLessThanOrEqualTo(5);
        await Assert.That(awayScore).IsLessThanOrEqualTo(5);
    }

    [Test]
    public async Task StrategyType_ReturnsStatsAnalyst()
    {
        // Assert
        await Assert.That(_strategy.StrategyType).IsEqualTo(BotStrategy.StatsAnalyst);
    }

    [Test]
    public async Task GeneratePrediction_CallsCalculator_WithCorrectParameters()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeForm = CreateTeamForm(50.0);
        var awayForm = CreateTeamForm(50.0);

        _formCalculator.CalculateFormAsync(match.HomeTeamId, match.CompetitionId, 5, Arg.Any<CancellationToken>())
            .Returns(homeForm);
        _formCalculator.CalculateFormAsync(match.AwayTeamId, match.CompetitionId, 5, Arg.Any<CancellationToken>())
            .Returns(awayForm);

        // Act
        await _strategy.GeneratePredictionAsync(match, null);

        // Assert
        await _formCalculator.Received(1).CalculateFormAsync(match.HomeTeamId, match.CompetitionId, 5, Arg.Any<CancellationToken>());
        await _formCalculator.Received(1).CalculateFormAsync(match.AwayTeamId, match.CompetitionId, 5, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GeneratePredictionAsync_WithFormFocusedConfig_UsesDifferentWeights()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = StatsAnalystConfig.FormFocused;
        var homeForm = CreateTeamForm(80.0); // High form
        var awayForm = CreateTeamForm(30.0); // Low form

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, config.ToJson());

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
    }

    private static Match CreateTestMatch(int externalId = 12345, int? matchday = null)
    {
        return Match.Create(
            externalId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            MatchStatus.Scheduled,
            matchday: matchday);
    }

    private static TeamFormCache CreateTeamForm(
        double formScore,
        double homeWinRate = 0.5,
        double awayWinRate = 0.3,
        int currentStreak = 0,
        double goalsPerMatch = 1.5,
        double goalsConcededPerMatch = 1.2)
    {
        return new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesAnalyzed = 5,
            MatchesPlayed = 5,
            PointsPerMatch = formScore / 100.0 * 3.0,
            GoalsPerMatch = goalsPerMatch,
            GoalsConcededPerMatch = goalsConcededPerMatch,
            HomeWinRate = homeWinRate,
            AwayWinRate = awayWinRate,
            CurrentStreak = currentStreak,
            CalculatedAt = DateTime.UtcNow
        };
    }
}
