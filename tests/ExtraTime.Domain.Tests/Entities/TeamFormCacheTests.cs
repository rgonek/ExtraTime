using ExtraTime.Domain.Entities;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class TeamFormCacheTests
{
    [Test]
    public async Task GetFormScore_WithPositiveRecord_ReturnsHigherScore()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 10,
            Wins = 8,
            Draws = 1,
            Losses = 1,
            PointsPerMatch = 2.5
        };

        // Act
        var formScore = cache.GetFormScore();

        // Assert
        await Assert.That(formScore).IsGreaterThan(80.0); // (2.5/3) * 100 = 83.33
    }

    [Test]
    public async Task GetFormScore_WithNegativeRecord_ReturnsLowerScore()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 10,
            Wins = 1,
            Draws = 2,
            Losses = 7,
            PointsPerMatch = 0.5
        };

        // Act
        var formScore = cache.GetFormScore();

        // Assert
        await Assert.That(formScore).IsLessThan(20.0); // (0.5/3) * 100 = 16.67
    }

    [Test]
    public async Task GetFormScore_WithNoMatches_ReturnsDefaultScore()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 0,
            PointsPerMatch = 0
        };

        // Act
        var formScore = cache.GetFormScore();

        // Assert
        await Assert.That(formScore).IsEqualTo(50.0);
    }

    [Test]
    public async Task GetHomeStrength_HomeWins_ReturnsHigherStrength()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            HomeMatchesPlayed = 10,
            HomeWins = 8,
            HomeWinRate = 0.8
        };

        // Act
        var homeStrength = cache.GetHomeStrength();

        // Assert
        await Assert.That(homeStrength).IsEqualTo(0.8);
    }

    [Test]
    public async Task GetHomeStrength_NoHomeMatches_ReturnsDefault()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            HomeMatchesPlayed = 0
        };

        // Act
        var homeStrength = cache.GetHomeStrength();

        // Assert
        await Assert.That(homeStrength).IsEqualTo(0.5);
    }

    [Test]
    public async Task GetAwayStrength_AwayWins_ReturnsHigherStrength()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            AwayMatchesPlayed = 10,
            AwayWins = 6,
            AwayWinRate = 0.6
        };

        // Act
        var awayStrength = cache.GetAwayStrength();

        // Assert
        await Assert.That(awayStrength).IsEqualTo(0.6);
    }

    [Test]
    public async Task GetAwayStrength_NoAwayMatches_ReturnsDefault()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            AwayMatchesPlayed = 0
        };

        // Act
        var awayStrength = cache.GetAwayStrength();

        // Assert
        await Assert.That(awayStrength).IsEqualTo(0.3);
    }

    [Test]
    public async Task GetAttackStrength_MoreGoals_ReturnsHigherScore()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 10,
            GoalsScored = 25,
            GoalsPerMatch = 2.5
        };

        // Act
        var attackStrength = cache.GetAttackStrength();

        // Assert
        await Assert.That(attackStrength).IsEqualTo(2.5);
    }

    [Test]
    public async Task GetAttackStrength_NoMatches_ReturnsDefault()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 0
        };

        // Act
        var attackStrength = cache.GetAttackStrength();

        // Assert
        await Assert.That(attackStrength).IsEqualTo(1.5);
    }

    [Test]
    public async Task GetDefenseStrength_FewerConceded_ReturnsLowerScore()
    {
        // Arrange - Fewer conceded = better defense (lower value)
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 10,
            GoalsConceded = 10,
            GoalsConcededPerMatch = 1.0
        };

        // Act
        var defenseStrength = cache.GetDefenseStrength();

        // Assert
        await Assert.That(defenseStrength).IsEqualTo(1.0);
    }

    [Test]
    public async Task GetDefenseStrength_MoreConceded_ReturnsHigherScore()
    {
        // Arrange - More conceded = worse defense (higher value)
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 10,
            GoalsConceded = 30,
            GoalsConcededPerMatch = 3.0
        };

        // Act
        var defenseStrength = cache.GetDefenseStrength();

        // Assert
        await Assert.That(defenseStrength).IsEqualTo(3.0);
    }

    [Test]
    public async Task GetDefenseStrength_NoMatches_ReturnsDefault()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 0
        };

        // Act
        var defenseStrength = cache.GetDefenseStrength();

        // Assert
        await Assert.That(defenseStrength).IsEqualTo(1.5);
    }

    [Test]
    public async Task Calculate_WithInsufficientData_HandlesGracefully()
    {
        // Arrange - Only 1 match played
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 1,
            Wins = 1,
            PointsPerMatch = 3.0
        };

        // Act
        var formScore = cache.GetFormScore();
        var homeStrength = cache.GetHomeStrength();
        var awayStrength = cache.GetAwayStrength();

        // Assert - Should still work with minimal data
        await Assert.That(formScore).IsEqualTo(100.0); // Perfect form
        await Assert.That(homeStrength).IsEqualTo(0.5); // Default
        await Assert.That(awayStrength).IsEqualTo(0.3); // Default
    }

    [Test]
    public async Task Calculate_WithWinStreak_HighFormScore()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 5,
            Wins = 5,
            Draws = 0,
            Losses = 0,
            PointsPerMatch = 3.0,
            CurrentStreak = 5,
            RecentForm = "WWWWW"
        };

        // Act
        var formScore = cache.GetFormScore();

        // Assert
        await Assert.That(formScore).IsEqualTo(100.0);
    }

    [Test]
    public async Task Calculate_WithLossStreak_LowFormScore()
    {
        // Arrange
        var cache = new TeamFormCache
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            MatchesPlayed = 5,
            Wins = 0,
            Draws = 0,
            Losses = 5,
            PointsPerMatch = 0.0,
            CurrentStreak = -5,
            RecentForm = "LLLLL"
        };

        // Act
        var formScore = cache.GetFormScore();

        // Assert
        await Assert.That(formScore).IsEqualTo(0.0);
    }
}
