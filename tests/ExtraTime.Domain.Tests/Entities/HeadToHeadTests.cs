using ExtraTime.Domain.Entities;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class HeadToHeadTests
{
    [Test]
    public async Task Create_WithSameTeamIds_ThrowsArgumentException()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act & Assert
        await Assert.That(() => HeadToHead.Create(teamId, teamId))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithUnorderedTeamIds_OrdersIdsAndSetsCompetition()
    {
        // Arrange
        var lowerTeamId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var higherTeamId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var competitionId = Guid.NewGuid();

        // Act
        var headToHead = HeadToHead.Create(higherTeamId, lowerTeamId, competitionId);

        // Assert
        await Assert.That(headToHead.Team1Id).IsEqualTo(lowerTeamId);
        await Assert.That(headToHead.Team2Id).IsEqualTo(higherTeamId);
        await Assert.That(headToHead.CompetitionId).IsEqualTo(competitionId);
    }

    [Test]
    public async Task UpdateStats_GetStatsForTeam1_ReturnsExpectedPerspective()
    {
        // Arrange
        var team1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var team2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var headToHead = HeadToHead.Create(team1Id, team2Id);
        var lastMatchId = Guid.NewGuid();
        var lastMatchDate = DateTime.UtcNow.AddDays(-1);

        // Act
        headToHead.UpdateStats(
            totalMatches: 4,
            team1Wins: 2,
            team2Wins: 1,
            draws: 1,
            team1Goals: 6,
            team2Goals: 4,
            team1HomeMatches: 2,
            team1HomeWins: 1,
            team1HomeGoals: 3,
            team1HomeConceded: 2,
            bothTeamsScoredCount: 3,
            over25Count: 2,
            lastMatchDate: lastMatchDate,
            lastMatchId: lastMatchId,
            recentMatchesCount: 3,
            recentTeam1Wins: 1,
            recentTeam2Wins: 1,
            recentDraws: 1,
            matchesAnalyzed: 4);
        var team1Stats = headToHead.GetStatsForTeam(team1Id);

        // Assert
        await Assert.That(team1Stats.Wins).IsEqualTo(2);
        await Assert.That(team1Stats.Losses).IsEqualTo(1);
        await Assert.That(team1Stats.Draws).IsEqualTo(1);
        await Assert.That(team1Stats.GoalsFor).IsEqualTo(6);
        await Assert.That(team1Stats.GoalsAgainst).IsEqualTo(4);
        await Assert.That(team1Stats.HomeMatches).IsEqualTo(2);
        await Assert.That(team1Stats.HomeWins).IsEqualTo(1);
        await Assert.That(team1Stats.RecentWins).IsEqualTo(1);
        await Assert.That(team1Stats.BttsRate).IsEqualTo(0.75d);
        await Assert.That(team1Stats.Over25Rate).IsEqualTo(0.5d);
        await Assert.That(team1Stats.WinRate).IsEqualTo(0.5d);
        await Assert.That(team1Stats.GoalDifference).IsEqualTo(2);
        await Assert.That(headToHead.LastMatchDate).IsEqualTo(lastMatchDate);
        await Assert.That(headToHead.LastMatchId).IsEqualTo(lastMatchId);
    }

    [Test]
    public async Task GetStatsForTeam_WhenTeamIsNotPartOfRecord_ThrowsArgumentException()
    {
        // Arrange
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var headToHead = HeadToHead.Create(team1Id, team2Id);

        // Act & Assert
        await Assert.That(() => headToHead.GetStatsForTeam(Guid.NewGuid()))
            .Throws<ArgumentException>();
    }
}
