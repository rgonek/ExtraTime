using ExtraTime.Domain.Entities;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class TeamXgStatsTests
{
    [Test]
    public async Task GetXgStrength_WithPositiveXgPerMatch_ReturnsScaledValue()
    {
        // Arrange
        var stats = new TeamXgStats
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            Season = "2025",
            XgPerMatch = 2.1
        };

        // Act
        var strength = stats.GetXgStrength();

        // Assert
        await Assert.That(strength).IsGreaterThan(1.39);
        await Assert.That(strength).IsLessThan(1.41);
    }

    [Test]
    public async Task GetDefensiveXgStrength_WithPositiveXgAgainstPerMatch_ReturnsInverseScaledValue()
    {
        // Arrange
        var stats = new TeamXgStats
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            Season = "2025",
            XgAgainstPerMatch = 1.2
        };

        // Act
        var strength = stats.GetDefensiveXgStrength();

        // Assert
        await Assert.That(strength).IsGreaterThan(1.24);
        await Assert.That(strength).IsLessThan(1.26);
    }

    [Test]
    public async Task IsOverperforming_WithPositiveOverperformance_ReturnsTrue()
    {
        // Arrange
        var stats = new TeamXgStats
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            Season = "2025",
            XgOverperformance = 0.7
        };

        // Act & Assert
        await Assert.That(stats.IsOverperforming()).IsTrue();
    }

    [Test]
    public async Task IsDefensivelySound_WithPositiveXgaOverperformance_ReturnsTrue()
    {
        // Arrange
        var stats = new TeamXgStats
        {
            TeamId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            Season = "2025",
            XgaOverperformance = 0.6
        };

        // Act & Assert
        await Assert.That(stats.IsDefensivelySound()).IsTrue();
    }
}
