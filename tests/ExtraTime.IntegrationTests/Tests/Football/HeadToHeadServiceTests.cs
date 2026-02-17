using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services.Football;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Football;

public sealed class HeadToHeadServiceTests : IntegrationTestBase
{
    [Test]
    public async Task GetOrCalculateAsync_WithFinishedMatches_ComputesExpectedStats()
    {
        // Arrange
        var service = CreateService();
        var (competition, team1, team2) = await SeedCompetitionAndTeamsAsync();
        var now = DateTime.UtcNow;

        Context.Matches.AddRange(
            new MatchBuilder()
                .WithExternalId(1001)
                .WithCompetitionId(competition.Id)
                .WithTeams(team1.Id, team2.Id)
                .WithMatchDate(now.AddDays(-3))
                .WithStatus(MatchStatus.Finished)
                .WithScore(2, 1)
                .Build(),
            new MatchBuilder()
                .WithExternalId(1002)
                .WithCompetitionId(competition.Id)
                .WithTeams(team2.Id, team1.Id)
                .WithMatchDate(now.AddDays(-2))
                .WithStatus(MatchStatus.Finished)
                .WithScore(0, 0)
                .Build(),
            new MatchBuilder()
                .WithExternalId(1003)
                .WithCompetitionId(competition.Id)
                .WithTeams(team1.Id, team2.Id)
                .WithMatchDate(now.AddDays(-1))
                .WithStatus(MatchStatus.Finished)
                .WithScore(1, 3)
                .Build(),
            new MatchBuilder()
                .WithExternalId(1004)
                .WithCompetitionId(competition.Id)
                .WithTeams(team1.Id, team2.Id)
                .WithMatchDate(now.AddDays(1))
                .WithStatus(MatchStatus.Scheduled)
                .Build());
        await Context.SaveChangesAsync();

        // Act
        var result = await service.GetOrCalculateAsync(team2.Id, team1.Id, competition.Id);

        // Assert
        await Assert.That(result.Team1Id).IsEqualTo(team1.Id);
        await Assert.That(result.Team2Id).IsEqualTo(team2.Id);
        await Assert.That(result.TotalMatches).IsEqualTo(3);
        await Assert.That(result.Team1Wins).IsEqualTo(1);
        await Assert.That(result.Team2Wins).IsEqualTo(1);
        await Assert.That(result.Draws).IsEqualTo(1);
        await Assert.That(result.Team1Goals).IsEqualTo(3);
        await Assert.That(result.Team2Goals).IsEqualTo(4);
        await Assert.That(result.BothTeamsScoredCount).IsEqualTo(2);
        await Assert.That(result.Over25Count).IsEqualTo(2);
        await Assert.That(result.Team1HomeMatches).IsEqualTo(2);
        await Assert.That(result.Team1HomeWins).IsEqualTo(1);
        await Assert.That(result.RecentMatchesCount).IsEqualTo(3);
        await Assert.That(result.RecentTeam1Wins).IsEqualTo(1);
        await Assert.That(result.RecentTeam2Wins).IsEqualTo(1);
        await Assert.That(result.RecentDraws).IsEqualTo(1);
        await Assert.That(result.LastMatchDate).IsNotNull();
        await Assert.That(result.LastMatchDate!.Value).IsGreaterThan(now.AddDays(-1).AddSeconds(-1));
        await Assert.That(result.LastMatchDate!.Value).IsLessThan(now.AddDays(-1).AddSeconds(1));
        await Assert.That(Context.HeadToHeads.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task GetOrCalculateAsync_WhenFreshCacheExists_DoesNotRecalculate()
    {
        // Arrange
        var service = CreateService();
        var (competition, team1, team2) = await SeedCompetitionAndTeamsAsync();
        var now = DateTime.UtcNow;

        Context.Matches.Add(new MatchBuilder()
            .WithExternalId(2001)
            .WithCompetitionId(competition.Id)
            .WithTeams(team1.Id, team2.Id)
            .WithMatchDate(now.AddDays(-2))
            .WithStatus(MatchStatus.Finished)
            .WithScore(1, 0)
            .Build());
        await Context.SaveChangesAsync();

        var cached = await service.GetOrCalculateAsync(team1.Id, team2.Id, competition.Id);

        Context.Matches.Add(new MatchBuilder()
            .WithExternalId(2002)
            .WithCompetitionId(competition.Id)
            .WithTeams(team1.Id, team2.Id)
            .WithMatchDate(now.AddDays(-1))
            .WithStatus(MatchStatus.Finished)
            .WithScore(2, 1)
            .Build());
        await Context.SaveChangesAsync();

        // Act
        var result = await service.GetOrCalculateAsync(team1.Id, team2.Id, competition.Id);

        // Assert
        await Assert.That(cached.TotalMatches).IsEqualTo(1);
        await Assert.That(result.TotalMatches).IsEqualTo(1);
        await Assert.That(result.Id).IsEqualTo(cached.Id);
        await Assert.That(Context.HeadToHeads.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task RefreshAsync_WhenCacheExists_RecalculatesWithLatestMatches()
    {
        // Arrange
        var service = CreateService();
        var (competition, team1, team2) = await SeedCompetitionAndTeamsAsync();
        var now = DateTime.UtcNow;

        Context.Matches.Add(new MatchBuilder()
            .WithExternalId(3001)
            .WithCompetitionId(competition.Id)
            .WithTeams(team1.Id, team2.Id)
            .WithMatchDate(now.AddDays(-2))
            .WithStatus(MatchStatus.Finished)
            .WithScore(1, 1)
            .Build());
        await Context.SaveChangesAsync();

        var cached = await service.GetOrCalculateAsync(team1.Id, team2.Id, competition.Id);
        var cachedTotalMatches = cached.TotalMatches;

        Context.Matches.Add(new MatchBuilder()
            .WithExternalId(3002)
            .WithCompetitionId(competition.Id)
            .WithTeams(team2.Id, team1.Id)
            .WithMatchDate(now.AddDays(-1))
            .WithStatus(MatchStatus.Finished)
            .WithScore(0, 2)
            .Build());
        await Context.SaveChangesAsync();

        // Act
        var refreshed = await service.RefreshAsync(team1.Id, team2.Id, competition.Id);

        // Assert
        await Assert.That(cachedTotalMatches).IsEqualTo(1);
        await Assert.That(refreshed.TotalMatches).IsEqualTo(2);
        await Assert.That(refreshed.Team1Wins).IsEqualTo(1);
        await Assert.That(refreshed.Team2Wins).IsEqualTo(0);
        await Assert.That(refreshed.Draws).IsEqualTo(1);
        await Assert.That(refreshed.Id).IsEqualTo(cached.Id);
    }

    [Test]
    public async Task GetOrCalculateAsync_WithSameTeamId_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var teamId = Guid.NewGuid();

        // Act & Assert
        await Assert.That(async () => await service.GetOrCalculateAsync(teamId, teamId))
            .Throws<ArgumentException>();
    }

    private HeadToHeadService CreateService()
    {
        var logger = Substitute.For<ILogger<HeadToHeadService>>();
        return new HeadToHeadService(Context, logger);
    }

    private async Task<(Competition Competition, Team Team1, Team Team2)> SeedCompetitionAndTeamsAsync()
    {
        var competition = new CompetitionBuilder()
            .WithId(Guid.Parse("10000000-0000-0000-0000-000000000001"))
            .WithExternalId(2021)
            .WithName("Premier League")
            .WithCode("PL")
            .Build();

        var team1 = new TeamBuilder()
            .WithId(Guid.Parse("00000000-0000-0000-0000-000000000001"))
            .WithExternalId(1)
            .WithName("Arsenal")
            .WithShortName("ARS")
            .Build();

        var team2 = new TeamBuilder()
            .WithId(Guid.Parse("00000000-0000-0000-0000-000000000002"))
            .WithExternalId(2)
            .WithName("Chelsea")
            .WithShortName("CHE")
            .Build();

        Context.Competitions.Add(competition);
        Context.Teams.AddRange(team1, team2);
        await Context.SaveChangesAsync();

        return (competition, team1, team2);
    }
}
