using ExtraTime.Domain.Entities;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class TeamTests
{
    [Test]
    public async Task Create_WithValidData_CreatesTeam()
    {
        // Arrange
        var externalId = 12345;
        var name = "Manchester United";
        var shortName = "Man Utd";
        var tla = "MUN";
        var logoUrl = "https://example.com/logo.png";
        var clubColors = "Red / White";
        var venue = "Old Trafford";

        // Act
        var team = Team.Create(externalId, name, shortName, tla, logoUrl, clubColors, venue);

        // Assert
        await Assert.That(team.ExternalId).IsEqualTo(externalId);
        await Assert.That(team.Name).IsEqualTo(name);
        await Assert.That(team.ShortName).IsEqualTo(shortName);
        await Assert.That(team.Tla).IsEqualTo(tla);
        await Assert.That(team.LogoUrl).IsEqualTo(logoUrl);
        await Assert.That(team.ClubColors).IsEqualTo(clubColors);
        await Assert.That(team.Venue).IsEqualTo(venue);
        await Assert.That(team.LastSyncedAt).IsNotDefault();
    }

    [Test]
    public async Task Create_WithMinimalData_CreatesTeam()
    {
        // Arrange
        var externalId = 12345;
        var name = "Manchester United";
        var shortName = "Man Utd";

        // Act
        var team = Team.Create(externalId, name, shortName);

        // Assert
        await Assert.That(team.ExternalId).IsEqualTo(externalId);
        await Assert.That(team.Name).IsEqualTo(name);
        await Assert.That(team.ShortName).IsEqualTo(shortName);
        await Assert.That(team.Tla).IsNull();
        await Assert.That(team.LogoUrl).IsNull();
        await Assert.That(team.ClubColors).IsNull();
        await Assert.That(team.Venue).IsNull();
    }

    [Test]
    public async Task Create_WithZeroExternalId_ThrowsArgumentException()
    {
        // Arrange
        var name = "Manchester United";
        var shortName = "Man Utd";

        // Act & Assert
        await Assert.That(() => Team.Create(0, name, shortName))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithNegativeExternalId_ThrowsArgumentException()
    {
        // Arrange
        var name = "Manchester United";
        var shortName = "Man Utd";

        // Act & Assert
        await Assert.That(() => Team.Create(-1, name, shortName))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var shortName = "Man Utd";

        // Act & Assert
        await Assert.That(() => Team.Create(12345, "", shortName))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithEmptyShortName_ThrowsArgumentException()
    {
        // Arrange
        var name = "Manchester United";

        // Act & Assert
        await Assert.That(() => Team.Create(12345, name, ""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task UpdateDetails_UpdatesNameAndLogo()
    {
        // Arrange
        var team = Team.Create(12345, "Old Name", "Old Short", "TLA", "https://old.com/logo.png", "Old Colors", "Old Venue");

        // Act
        team.UpdateDetails("New Name", "New Short", "NEW", "https://new.com/logo.png", "New Colors", "New Venue");

        // Assert
        await Assert.That(team.Name).IsEqualTo("New Name");
        await Assert.That(team.ShortName).IsEqualTo("New Short");
        await Assert.That(team.Tla).IsEqualTo("NEW");
        await Assert.That(team.LogoUrl).IsEqualTo("https://new.com/logo.png");
        await Assert.That(team.ClubColors).IsEqualTo("New Colors");
        await Assert.That(team.Venue).IsEqualTo("New Venue");
    }

    [Test]
    public async Task UpdateDetails_WithNullOptionalParameters_AllowsNulls()
    {
        // Arrange
        var team = Team.Create(12345, "Name", "Short", "TLA", "https://logo.png", "Colors", "Venue");

        // Act
        team.UpdateDetails("New Name", "New Short", null, null, null, null);

        // Assert
        await Assert.That(team.Name).IsEqualTo("New Name");
        await Assert.That(team.Tla).IsNull();
        await Assert.That(team.LogoUrl).IsNull();
        await Assert.That(team.ClubColors).IsNull();
        await Assert.That(team.Venue).IsNull();
    }

    [Test]
    public async Task UpdateDetails_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var team = Team.Create(12345, "Name", "Short");

        // Act & Assert
        await Assert.That(() => team.UpdateDetails("", "New Short"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task UpdateDetails_WithEmptyShortName_ThrowsArgumentException()
    {
        // Arrange
        var team = Team.Create(12345, "Name", "Short");

        // Act & Assert
        await Assert.That(() => team.UpdateDetails("New Name", ""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task RecordSync_UpdatesLastSyncedAt()
    {
        // Arrange
        var team = Team.Create(12345, "Name", "Short");
        var originalSyncTime = team.LastSyncedAt;

        await Task.Delay(10);

        // Act
        team.RecordSync();

        // Assert
        await Assert.That(team.LastSyncedAt).IsNotEqualTo(originalSyncTime);
    }
}
