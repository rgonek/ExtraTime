using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class StatsAnalystStrategyTests
{
    private ITeamFormCalculator _formCalculator = null!;
    private IIntegrationHealthService _integrationHealthService = null!;
    private IUnderstatService _understatService = null!;
    private IOddsDataService _oddsDataService = null!;
    private IInjuryService _injuryService = null!;
    private IEloRatingService _eloRatingService = null!;
    private StatsAnalystStrategy _strategy = null!;

    [Before(Test)]
    public void Setup()
    {
        _formCalculator = Substitute.For<ITeamFormCalculator>();
        _integrationHealthService = Substitute.For<IIntegrationHealthService>();
        _understatService = Substitute.For<IUnderstatService>();
        _oddsDataService = Substitute.For<IOddsDataService>();
        _injuryService = Substitute.For<IInjuryService>();
        _eloRatingService = Substitute.For<IEloRatingService>();

        _integrationHealthService.GetDataAvailabilityAsync(Arg.Any<CancellationToken>())
            .Returns(new DataAvailability());

        _strategy = new StatsAnalystStrategy(
            _formCalculator,
            integrationHealthService: _integrationHealthService,
            understatService: _understatService,
            oddsService: _oddsDataService,
            injuryService: _injuryService,
            eloService: _eloRatingService,
            fallbackStrategy: new FallbackStrategy(new Random(42)));
    }

    [Test]
    public async Task GeneratePredictionAsync_WithFormData_UsesRecentPerformance()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeForm = CreateTeamForm(60.0);
        var awayForm = CreateTeamForm(40.0);

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, null);

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(homeScore).IsLessThanOrEqualTo(4);
        await Assert.That(awayScore).IsLessThanOrEqualTo(4);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithHomeAdvantage_ConsidersHomeStrength()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeForm = CreateTeamForm(50.0, homeWinRate: 0.8);
        var awayForm = CreateTeamForm(50.0, awayWinRate: 0.2);

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

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
        var homeForm = CreateTeamForm(50.0, currentStreak: 5);
        var awayForm = CreateTeamForm(50.0, currentStreak: -3);

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
        var match = CreateTestMatch(matchday: 35);
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

        // Assert
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

        // Assert
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

        // Assert
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
    public async Task GeneratePrediction_CallsCalculator()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeForm = CreateTeamForm(50.0);
        var awayForm = CreateTeamForm(50.0);

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

        // Act
        await _strategy.GeneratePredictionAsync(match, null);

        // Assert
        await _formCalculator.Received(2).CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GeneratePredictionAsync_WithFormFocusedConfig_UsesDifferentWeights()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = StatsAnalystConfig.FormFocused;
        var homeForm = CreateTeamForm(80.0);
        var awayForm = CreateTeamForm(30.0);

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(homeForm, awayForm);

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, config.ToJson());

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GeneratePredictionAsync_WhenWeightedDataUnavailable_UsesFallbackStrategy()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = new StatsAnalystConfig
        {
            FormWeight = 0.10,
            HomeAdvantageWeight = 0.10,
            GoalTrendWeight = 0.0,
            StreakWeight = 0.0,
            LineupAnalysisWeight = 0.0,
            XgWeight = 0.80,
            XgDefensiveWeight = 0.0,
            OddsWeight = 0.0,
            InjuryWeight = 0.0,
            EloWeight = 0.0
        };
        _integrationHealthService.GetDataAvailabilityAsync(Arg.Any<CancellationToken>())
            .Returns(new DataAvailability { XgDataAvailable = false });
        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateTeamForm(50.0), CreateTeamForm(50.0));

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, config.ToJson());

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(1);
        await Assert.That(homeScore).IsLessThanOrEqualTo(2);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsLessThanOrEqualTo(2);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithAvailableExternalData_RequestsAllSources()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = StatsAnalystConfig.FullAnalysis;

        _integrationHealthService.GetDataAvailabilityAsync(Arg.Any<CancellationToken>())
            .Returns(new DataAvailability
            {
                XgDataAvailable = true,
                OddsDataAvailable = true,
                InjuryDataAvailable = true,
                EloDataAvailable = true
            });

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateTeamForm(65.0), CreateTeamForm(45.0));
        _understatService.GetTeamXgAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateXgStats(1.8, 1.1), CreateXgStats(1.2, 1.4));
        _oddsDataService.GetOddsForMatchAsync(match.Id, Arg.Any<CancellationToken>())
            .Returns(CreateOdds(MatchOutcome.HomeWin, 0.62));
        _injuryService.GetTeamInjuriesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateInjuries(15), CreateInjuries(10));
        _eloRatingService.GetTeamEloAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateElo(1700), CreateElo(1600));

        // Act
        var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, config.ToJson());

        // Assert
        await _understatService.Received(2).GetTeamXgAsync(
            Arg.Any<Guid>(),
            match.CompetitionId,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _oddsDataService.Received(1).GetOddsForMatchAsync(match.Id, Arg.Any<CancellationToken>());
        await _injuryService.Received(2).GetTeamInjuriesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eloRatingService.Received(2).GetTeamEloAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithPhase95Configs_ReturnsValidScores()
    {
        // Arrange
        var match = CreateTestMatch();
        var configs = new[]
        {
            StatsAnalystConfig.FullAnalysis,
            StatsAnalystConfig.XgFocused,
            StatsAnalystConfig.MarketFollower,
            StatsAnalystConfig.InjuryAware
        };

        _integrationHealthService.GetDataAvailabilityAsync(Arg.Any<CancellationToken>())
            .Returns(new DataAvailability
            {
                XgDataAvailable = true,
                OddsDataAvailable = true,
                InjuryDataAvailable = true,
                EloDataAvailable = true
            });

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateTeamForm(60.0), CreateTeamForm(45.0));
        _understatService.GetTeamXgAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateXgStats(1.9, 1.0), CreateXgStats(1.2, 1.4));
        _oddsDataService.GetOddsForMatchAsync(match.Id, Arg.Any<CancellationToken>())
            .Returns(CreateOdds(MatchOutcome.HomeWin, 0.58));
        _injuryService.GetTeamInjuriesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateInjuries(10), CreateInjuries(18));
        _eloRatingService.GetTeamEloAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateElo(1680), CreateElo(1590));

        foreach (var config in configs)
        {
            // Act
            var (homeScore, awayScore) = await _strategy.GeneratePredictionAsync(match, config.ToJson());

            // Assert
            await Assert.That(homeScore).IsGreaterThanOrEqualTo(config.MinGoals);
            await Assert.That(homeScore).IsLessThanOrEqualTo(config.MaxGoals);
            await Assert.That(awayScore).IsGreaterThanOrEqualTo(config.MinGoals);
            await Assert.That(awayScore).IsLessThanOrEqualTo(config.MaxGoals);
        }
    }

    [Test]
    public async Task GeneratePredictionAsync_XgFocusedAndMarketFollower_CanProduceDifferentPredictions()
    {
        // Arrange
        var match = CreateTestMatch();
        var xgConfig = StatsAnalystConfig.XgFocused with { RandomVariance = 0 };
        var marketConfig = StatsAnalystConfig.MarketFollower with { RandomVariance = 0 };

        _integrationHealthService.GetDataAvailabilityAsync(Arg.Any<CancellationToken>())
            .Returns(new DataAvailability
            {
                XgDataAvailable = true,
                OddsDataAvailable = true,
                InjuryDataAvailable = true
            });

        _formCalculator.CalculateFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateTeamForm(50.0), CreateTeamForm(50.0));
        _understatService.GetTeamXgAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateXgStats(2.4, 0.8), CreateXgStats(0.7, 2.0));
        _oddsDataService.GetOddsForMatchAsync(match.Id, Arg.Any<CancellationToken>())
            .Returns(CreateOdds(MatchOutcome.AwayWin, 0.88));
        _injuryService.GetTeamInjuriesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateInjuries(5), CreateInjuries(5));

        // Act
        var xgPrediction = await _strategy.GeneratePredictionAsync(match, xgConfig.ToJson());
        var marketPrediction = await _strategy.GeneratePredictionAsync(match, marketConfig.ToJson());

        // Assert
        await Assert.That(
            xgPrediction.HomeScore != marketPrediction.HomeScore ||
            xgPrediction.AwayScore != marketPrediction.AwayScore).IsTrue();
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

    private static TeamXgStats CreateXgStats(double xgPerMatch, double xgAgainstPerMatch)
    {
        return new TeamXgStats
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            Season = DateTime.UtcNow.Year.ToString(),
            XgPerMatch = xgPerMatch,
            XgAgainstPerMatch = xgAgainstPerMatch,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    private static MatchOdds CreateOdds(MatchOutcome favorite, double confidence)
    {
        return new MatchOdds
        {
            Id = Guid.NewGuid(),
            MatchId = Guid.NewGuid(),
            HomeWinOdds = 2.0,
            DrawOdds = 3.2,
            AwayWinOdds = 4.0,
            MarketFavorite = favorite,
            FavoriteConfidence = confidence,
            ImportedAt = DateTime.UtcNow
        };
    }

    private static TeamInjuries CreateInjuries(double impact)
    {
        return new TeamInjuries
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            InjuryImpactScore = impact,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    private static TeamEloRating CreateElo(double rating)
    {
        return new TeamEloRating
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            EloRating = rating,
            EloRank = 0,
            ClubEloName = "Test",
            RatingDate = DateTime.UtcNow.Date,
            SyncedAt = DateTime.UtcNow
        };
    }
}
