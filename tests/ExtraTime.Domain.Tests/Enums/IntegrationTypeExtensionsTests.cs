using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Tests.Enums;

public sealed class IntegrationTypeExtensionsTests
{
    [Test]
    public async Task ToName_ForClubElo_ShouldReturnExpectedDisplayName()
    {
        // Act
        var name = IntegrationType.ClubElo.ToName();

        // Assert
        await Assert.That(name).IsEqualTo("ClubElo.com");
    }

    [Test]
    public async Task GetStaleThreshold_ForFootballDataUk_ShouldReturnSevenDays()
    {
        // Act
        var threshold = IntegrationType.FootballDataUk.GetStaleThreshold();

        // Assert
        await Assert.That(threshold).IsEqualTo(TimeSpan.FromDays(7));
    }

    [Test]
    public async Task ToName_ForLineupProvider_ShouldReturnExpectedDisplayName()
    {
        // Act
        var name = IntegrationType.LineupProvider.ToName();

        // Assert
        await Assert.That(name).IsEqualTo("Lineup Provider");
    }
}
