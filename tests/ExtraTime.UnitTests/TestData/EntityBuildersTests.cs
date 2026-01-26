namespace ExtraTime.UnitTests.TestData;

public sealed class EntityBuildersTests
{
    [Test]
    public async Task UserBuilder_ShouldCreateValidUser()
    {
        // Act
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .WithUsername("testuser")
            .Build();

        // Assert
        await Assert.That(user.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(user.Email).IsEqualTo("test@example.com");
        await Assert.That(user.Username).IsEqualTo("testuser");
        await Assert.That(user.PasswordHash).IsNotNull();
    }

    [Test]
    public async Task LeagueBuilder_ShouldCreateValidLeague()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var league = new LeagueBuilder()
            .WithName("Test League")
            .WithOwnerId(ownerId)
            .WithScoringRules(5, 2)
            .Build();

        // Assert
        await Assert.That(league.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(league.Name).IsEqualTo("Test League");
        await Assert.That(league.OwnerId).IsEqualTo(ownerId);
        await Assert.That(league.ScoreExactMatch).IsEqualTo(5);
        await Assert.That(league.ScoreCorrectResult).IsEqualTo(2);
    }

    [Test]
    public async Task MatchBuilder_ShouldCreateValidMatch()
    {
        // Act
        var match = new MatchBuilder()
            .WithStatus(Domain.Enums.MatchStatus.Finished)
            .WithScore(2, 1)
            .Build();

        // Assert
        await Assert.That(match.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(match.Status).IsEqualTo(Domain.Enums.MatchStatus.Finished);
        await Assert.That(match.HomeScore).IsEqualTo(2);
        await Assert.That(match.AwayScore).IsEqualTo(1);
    }

    [Test]
    public async Task BetBuilder_ShouldCreateValidBet()
    {
        // Act
        var bet = new BetBuilder()
            .WithPrediction(3, 1)
            .Build();

        // Assert
        await Assert.That(bet.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(bet.PredictedHomeScore).IsEqualTo(3);
        await Assert.That(bet.PredictedAwayScore).IsEqualTo(1);
    }
}
