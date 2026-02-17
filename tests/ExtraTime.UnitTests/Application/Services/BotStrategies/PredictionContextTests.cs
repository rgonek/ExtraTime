using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class PredictionContextTests
{
    [Test]
    public async Task CalculateEffectiveWeights_WhenDataMissing_RedistributesAvailableWeights()
    {
        // Arrange
        var config = new StatsAnalystConfig
        {
            FormWeight = 0.25,
            HomeAdvantageWeight = 0.25,
            GoalTrendWeight = 0.0,
            StreakWeight = 0.0,
            LineupAnalysisWeight = 0.0,
            XgWeight = 0.50,
            XgDefensiveWeight = 0.0,
            OddsWeight = 0.0,
            InjuryWeight = 0.0,
            EloWeight = 0.0
        };
        var context = new PredictionContext
        {
            Match = CreateTestMatch(),
            Config = config,
            DataAvailability = new DataAvailability { XgDataAvailable = false },
            HomeForm = CreateForm(),
            AwayForm = CreateForm()
        };

        // Act
        var effective = context.CalculateEffectiveWeights();

        // Assert
        await Assert.That(effective.FormWeight).IsEqualTo(0.50);
        await Assert.That(effective.HomeAdvantageWeight).IsEqualTo(0.50);
        await Assert.That(effective.XgWeight).IsEqualTo(0.0);
        await Assert.That(effective.DataQualityScore).IsEqualTo(50.0);
    }

    [Test]
    public async Task CanMakePrediction_WhenDataQualityLow_ReturnsFalse()
    {
        // Arrange
        var config = new StatsAnalystConfig
        {
            FormWeight = 0.20,
            HomeAdvantageWeight = 0.10,
            GoalTrendWeight = 0.0,
            StreakWeight = 0.0,
            LineupAnalysisWeight = 0.0,
            XgWeight = 0.70,
            XgDefensiveWeight = 0.0,
            OddsWeight = 0.0,
            InjuryWeight = 0.0,
            EloWeight = 0.0
        };
        var context = new PredictionContext
        {
            Match = CreateTestMatch(),
            Config = config,
            DataAvailability = new DataAvailability { XgDataAvailable = false },
            HomeForm = CreateForm(),
            AwayForm = CreateForm()
        };

        // Act
        var canPredict = context.CanMakePrediction();

        // Assert
        await Assert.That(canPredict).IsFalse();
    }

    [Test]
    public async Task GetDegradationWarning_WhenWeightedSourceMissing_ReturnsWarningMessage()
    {
        // Arrange
        var config = new StatsAnalystConfig
        {
            XgWeight = 0.20,
            UseXgData = true
        };
        var context = new PredictionContext
        {
            Match = CreateTestMatch(),
            Config = config,
            DataAvailability = new DataAvailability { XgDataAvailable = false },
            HomeForm = CreateForm(),
            AwayForm = CreateForm()
        };

        // Act
        var warning = context.GetDegradationWarning();

        // Assert
        await Assert.That(warning).Contains("xG data unavailable");
    }

    [Test]
    public async Task CanUseElo_WhenAvailabilityAndRatingsPresent_ReturnsTrue()
    {
        // Arrange
        var context = new PredictionContext
        {
            Match = CreateTestMatch(),
            Config = new StatsAnalystConfig { UseEloData = true },
            DataAvailability = new DataAvailability { EloDataAvailable = true },
            HomeForm = CreateForm(),
            AwayForm = CreateForm(),
            HomeElo = CreateElo(),
            AwayElo = CreateElo()
        };

        // Assert
        await Assert.That(context.CanUseElo).IsTrue();
    }

    [Test]
    public async Task HasContextFlags_WhenOptionalDataIsPresent_ReturnsTrue()
    {
        // Arrange
        var context = new PredictionContext
        {
            Match = CreateTestMatch(),
            Config = new StatsAnalystConfig(),
            DataAvailability = new DataAvailability
            {
                SuspensionDataAvailable = true,
                WeatherDataAvailable = true,
                RefereeDataAvailable = true
            },
            HomeForm = CreateForm(),
            AwayForm = CreateForm(),
            HomeSuspensions = new TeamSuspensions { TeamId = Guid.NewGuid() },
            WeatherContext = new WeatherContextData(12, 30, 85, "Rain", true),
            RefereeProfile = new RefereeProfileData("Referee", 4.2, 25, 0.4)
        };

        // Assert
        await Assert.That(context.HasSuspensionContext).IsTrue();
        await Assert.That(context.HasWeatherContext).IsTrue();
        await Assert.That(context.HasRefereeContext).IsTrue();
    }

    private static Match CreateTestMatch()
    {
        return Match.Create(
            externalId: 999,
            homeTeamId: Guid.NewGuid(),
            awayTeamId: Guid.NewGuid(),
            competitionId: Guid.NewGuid(),
            matchDateUtc: DateTime.UtcNow.AddDays(1),
            status: MatchStatus.Scheduled);
    }

    private static TeamFormCache CreateForm()
    {
        return new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesAnalyzed = 5,
            MatchesPlayed = 5,
            PointsPerMatch = 1.5,
            GoalsPerMatch = 1.6,
            GoalsConcededPerMatch = 1.1,
            HomeWinRate = 0.55,
            AwayWinRate = 0.40,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private static TeamEloRating CreateElo()
    {
        return new TeamEloRating
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            EloRating = 1650,
            EloRank = 100,
            ClubEloName = "Test",
            RatingDate = DateTime.UtcNow.Date,
            SyncedAt = DateTime.UtcNow
        };
    }
}
