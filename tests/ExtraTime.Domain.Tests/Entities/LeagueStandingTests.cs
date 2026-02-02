using ExtraTime.Domain.Entities;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class LeagueStandingTests
{
    [Test]
    public async Task Create_WithValidData_CreatesStanding()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var standing = LeagueStanding.Create(leagueId, userId);

        // Assert
        await Assert.That(standing.LeagueId).IsEqualTo(leagueId);
        await Assert.That(standing.UserId).IsEqualTo(userId);
        await Assert.That(standing.TotalPoints).IsEqualTo(0);
        await Assert.That(standing.BetsPlaced).IsEqualTo(0);
        await Assert.That(standing.ExactMatches).IsEqualTo(0);
        await Assert.That(standing.CorrectResults).IsEqualTo(0);
        await Assert.That(standing.CurrentStreak).IsEqualTo(0);
        await Assert.That(standing.BestStreak).IsEqualTo(0);
        await Assert.That(standing.LastUpdatedAt).IsNotDefault();
    }

    [Test]
    public async Task ApplyBetResult_ExactMatch_UpdatesStatsAndStreak()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());
        var points = 5;
        var isExactMatch = true;
        var isCorrectResult = true;

        // Act
        standing.ApplyBetResult(points, isExactMatch, isCorrectResult);

        // Assert
        await Assert.That(standing.TotalPoints).IsEqualTo(points);
        await Assert.That(standing.BetsPlaced).IsEqualTo(1);
        await Assert.That(standing.ExactMatches).IsEqualTo(1);
        await Assert.That(standing.CorrectResults).IsEqualTo(1);
        await Assert.That(standing.CurrentStreak).IsEqualTo(1);
        await Assert.That(standing.BestStreak).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyBetResult_CorrectResult_UpdatesStatsAndStreak()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());
        var points = 2;
        var isExactMatch = false;
        var isCorrectResult = true;

        // Act
        standing.ApplyBetResult(points, isExactMatch, isCorrectResult);

        // Assert
        await Assert.That(standing.TotalPoints).IsEqualTo(points);
        await Assert.That(standing.BetsPlaced).IsEqualTo(1);
        await Assert.That(standing.ExactMatches).IsEqualTo(0);
        await Assert.That(standing.CorrectResults).IsEqualTo(1);
        await Assert.That(standing.CurrentStreak).IsEqualTo(1);
        await Assert.That(standing.BestStreak).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyBetResult_WrongResult_BreaksStreak()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());
        // First, build a streak
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(5, true, true);
        await Assert.That(standing.CurrentStreak).IsEqualTo(3);
        await Assert.That(standing.BestStreak).IsEqualTo(3);

        // Act - wrong result
        standing.ApplyBetResult(0, false, false);

        // Assert
        await Assert.That(standing.TotalPoints).IsEqualTo(15);
        await Assert.That(standing.BetsPlaced).IsEqualTo(4);
        await Assert.That(standing.CurrentStreak).IsEqualTo(0);
        await Assert.That(standing.BestStreak).IsEqualTo(3); // Best streak preserved
    }

    [Test]
    public async Task ApplyBetResult_MultipleBets_AccumulatesCorrectly()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act
        standing.ApplyBetResult(5, true, true);   // Exact match
        standing.ApplyBetResult(2, false, true);  // Correct result
        standing.ApplyBetResult(0, false, false); // Wrong result
        standing.ApplyBetResult(5, true, true);   // Exact match

        // Assert
        await Assert.That(standing.TotalPoints).IsEqualTo(12);
        await Assert.That(standing.BetsPlaced).IsEqualTo(4);
        await Assert.That(standing.ExactMatches).IsEqualTo(2);
        await Assert.That(standing.CorrectResults).IsEqualTo(3);
        await Assert.That(standing.CurrentStreak).IsEqualTo(1); // Started new streak
        await Assert.That(standing.BestStreak).IsEqualTo(2); // First two wins
    }

    [Test]
    public async Task ApplyBetResult_StreakTracking_WorksCorrectly()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());

        // Build streak to 3
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(5, true, true);
        await Assert.That(standing.BestStreak).IsEqualTo(3);

        // Break streak
        standing.ApplyBetResult(0, false, false);
        await Assert.That(standing.CurrentStreak).IsEqualTo(0);
        await Assert.That(standing.BestStreak).IsEqualTo(3);

        // Build new streak to 5
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(5, true, true);

        // Assert - best streak should now be 5
        await Assert.That(standing.CurrentStreak).IsEqualTo(5);
        await Assert.That(standing.BestStreak).IsEqualTo(5);
    }

    [Test]
    public async Task Reset_ClearsAllStatsAndPoints()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());
        standing.ApplyBetResult(5, true, true);
        standing.ApplyBetResult(2, false, true);
        standing.ApplyBetResult(5, true, true);
        await Assert.That(standing.TotalPoints).IsEqualTo(12);
        await Assert.That(standing.BetsPlaced).IsEqualTo(3);

        // Act
        standing.Reset();

        // Assert
        await Assert.That(standing.TotalPoints).IsEqualTo(0);
        await Assert.That(standing.BetsPlaced).IsEqualTo(0);
        await Assert.That(standing.ExactMatches).IsEqualTo(0);
        await Assert.That(standing.CorrectResults).IsEqualTo(0);
        await Assert.That(standing.CurrentStreak).IsEqualTo(0);
        await Assert.That(standing.BestStreak).IsEqualTo(0);
    }

    [Test]
    public async Task Reset_UpdatesLastUpdatedAt()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());
        var originalUpdateTime = standing.LastUpdatedAt;
        
        // Wait a tiny bit to ensure different timestamp
        await Task.Delay(10);

        // Act
        standing.Reset();

        // Assert
        await Assert.That(standing.LastUpdatedAt).IsNotEqualTo(originalUpdateTime);
    }

    [Test]
    public async Task ApplyBetResult_UpdatesLastUpdatedAt()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());
        var originalUpdateTime = standing.LastUpdatedAt;
        
        // Wait a tiny bit to ensure different timestamp
        await Task.Delay(10);

        // Act
        standing.ApplyBetResult(5, true, true);

        // Assert
        await Assert.That(standing.LastUpdatedAt).IsNotEqualTo(originalUpdateTime);
    }

    [Test]
    public async Task ApplyBetResult_ZeroPoints_DoesNotIncrementStreak()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act - multiple wrong results
        standing.ApplyBetResult(0, false, false);
        standing.ApplyBetResult(0, false, false);
        standing.ApplyBetResult(0, false, false);

        // Assert
        await Assert.That(standing.CurrentStreak).IsEqualTo(0);
        await Assert.That(standing.BestStreak).IsEqualTo(0);
        await Assert.That(standing.BetsPlaced).IsEqualTo(3);
    }

    [Test]
    public async Task ApplyBetResult_PartialPoints_StreakContinues()
    {
        // Arrange
        var standing = LeagueStanding.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act - correct results (not exact)
        standing.ApplyBetResult(2, false, true);
        standing.ApplyBetResult(2, false, true);

        // Assert
        await Assert.That(standing.CurrentStreak).IsEqualTo(2);
        await Assert.That(standing.CorrectResults).IsEqualTo(2);
        await Assert.That(standing.ExactMatches).IsEqualTo(0);
    }
}
