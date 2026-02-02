using ExtraTime.Domain.Entities;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class CompetitionTests
{
    [Test]
    public async Task Create_WithValidData_CreatesCompetition()
    {
        // Arrange
        var externalId = 2021;
        var name = "Premier League";
        var code = "PL";
        var country = "England";
        var logoUrl = "https://example.com/pl-logo.png";

        // Act
        var competition = Competition.Create(externalId, name, code, country, logoUrl);

        // Assert
        await Assert.That(competition.ExternalId).IsEqualTo(externalId);
        await Assert.That(competition.Name).IsEqualTo(name);
        await Assert.That(competition.Code).IsEqualTo(code);
        await Assert.That(competition.Country).IsEqualTo(country);
        await Assert.That(competition.LogoUrl).IsEqualTo(logoUrl);
        await Assert.That(competition.CurrentMatchday).IsNull();
        await Assert.That(competition.CurrentSeasonStart).IsNull();
        await Assert.That(competition.CurrentSeasonEnd).IsNull();
        await Assert.That(competition.LastSyncedAt).IsNotDefault();
    }

    [Test]
    public async Task Create_WithMinimalData_CreatesCompetition()
    {
        // Arrange
        var externalId = 2021;
        var name = "Premier League";
        var code = "PL";
        var country = "England";

        // Act
        var competition = Competition.Create(externalId, name, code, country);

        // Assert
        await Assert.That(competition.ExternalId).IsEqualTo(externalId);
        await Assert.That(competition.Name).IsEqualTo(name);
        await Assert.That(competition.Code).IsEqualTo(code);
        await Assert.That(competition.Country).IsEqualTo(country);
        await Assert.That(competition.LogoUrl).IsNull();
    }

    [Test]
    public async Task Create_WithZeroExternalId_ThrowsArgumentException()
    {
        // Arrange
        var name = "Premier League";
        var code = "PL";
        var country = "England";

        // Act & Assert
        await Assert.That(() => Competition.Create(0, name, code, country))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithNegativeExternalId_ThrowsArgumentException()
    {
        // Arrange
        var name = "Premier League";
        var code = "PL";
        var country = "England";

        // Act & Assert
        await Assert.That(() => Competition.Create(-1, name, code, country))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var code = "PL";
        var country = "England";

        // Act & Assert
        await Assert.That(() => Competition.Create(2021, "", code, country))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithEmptyCode_ThrowsArgumentException()
    {
        // Arrange
        var name = "Premier League";
        var country = "England";

        // Act & Assert
        await Assert.That(() => Competition.Create(2021, name, "", country))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithEmptyCountry_ThrowsArgumentException()
    {
        // Arrange
        var name = "Premier League";
        var code = "PL";

        // Act & Assert
        await Assert.That(() => Competition.Create(2021, name, code, ""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task UpdateDetails_UpdatesNameAndCountry()
    {
        // Arrange
        var competition = Competition.Create(2021, "Old Name", "OLD", "Old Country", "https://old.com/logo.png");
        
        // Act
        competition.UpdateDetails("New Name", "NEW", "New Country", "https://new.com/logo.png");

        // Assert
        await Assert.That(competition.Name).IsEqualTo("New Name");
        await Assert.That(competition.Code).IsEqualTo("NEW");
        await Assert.That(competition.Country).IsEqualTo("New Country");
        await Assert.That(competition.LogoUrl).IsEqualTo("https://new.com/logo.png");
    }

    [Test]
    public async Task UpdateDetails_WithNullLogo_AllowsNull()
    {
        // Arrange
        var competition = Competition.Create(2021, "Name", "CODE", "Country", "https://logo.png");
        
        // Act
        competition.UpdateDetails("New Name", "NEW", "New Country", null);

        // Assert
        await Assert.That(competition.LogoUrl).IsNull();
    }

    [Test]
    public async Task RecordSync_UpdatesLastSyncedAt()
    {
        // Arrange
        var competition = Competition.Create(2021, "Name", "CODE", "Country");
        var originalSyncTime = competition.LastSyncedAt;
        
        await Task.Delay(10);

        // Act
        competition.RecordSync();

        // Assert
        await Assert.That(competition.LastSyncedAt).IsNotEqualTo(originalSyncTime);
    }

    [Test]
    public async Task UpdateCurrentSeason_UpdatesSeasonDates()
    {
        // Arrange
        var competition = Competition.Create(2021, "Name", "CODE", "Country");
        var seasonStart = new DateTime(2024, 8, 1);
        var seasonEnd = new DateTime(2025, 5, 31);
        var currentMatchday = 15;

        // Act
        competition.UpdateCurrentSeason(currentMatchday, seasonStart, seasonEnd);

        // Assert
        await Assert.That(competition.CurrentMatchday).IsEqualTo(currentMatchday);
        await Assert.That(competition.CurrentSeasonStart).IsEqualTo(seasonStart);
        await Assert.That(competition.CurrentSeasonEnd).IsEqualTo(seasonEnd);
    }

    [Test]
    public async Task UpdateCurrentSeason_WithNullValues_AllowsNulls()
    {
        // Arrange
        var competition = Competition.Create(2021, "Name", "CODE", "Country");
        competition.UpdateCurrentSeason(10, DateTime.UtcNow, DateTime.UtcNow.AddMonths(9));

        // Act
        competition.UpdateCurrentSeason(null, null, null);

        // Assert
        await Assert.That(competition.CurrentMatchday).IsNull();
        await Assert.That(competition.CurrentSeasonStart).IsNull();
        await Assert.That(competition.CurrentSeasonEnd).IsNull();
    }

    [Test]
    public async Task UpdateCurrentSeason_UpdatesLastSyncedAt()
    {
        // Arrange
        var competition = Competition.Create(2021, "Name", "CODE", "Country");
        var originalSyncTime = competition.LastSyncedAt;
        
        await Task.Delay(10);

        // Act
        competition.UpdateCurrentSeason(15, DateTime.UtcNow, DateTime.UtcNow.AddMonths(9));

        // Assert
        await Assert.That(competition.LastSyncedAt).IsNotEqualTo(originalSyncTime);
    }
}
