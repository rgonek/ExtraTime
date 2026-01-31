using ExtraTime.Application.Features.Football.Queries.GetMatches;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Football;

[TestCategory(TestCategories.Significant)]
public sealed class GetMatchesQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetMatches_NoMatches_ReturnsEmptyPage()
    {
        // Arrange
        var handler = new GetMatchesQueryHandler(Context);
        var query = new GetMatchesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(0);
        await Assert.That(result.Value.TotalCount).IsEqualTo(0);
        await Assert.That(result.Value.Page).IsEqualTo(1);
        await Assert.That(result.Value.TotalPages).IsEqualTo(0);
    }

    [Test]
    public async Task GetMatches_WithMatches_ReturnsPagedResults()
    {
        // Arrange
        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);
        await Context.SaveChangesAsync();

        var matches = new List<Domain.Entities.Match>();
        for (int i = 0; i < 25; i++)
        {
            var match = new MatchBuilder()
                .WithCompetitionId(competition.Id)
                .WithTeams(homeTeam.Id, awayTeam.Id)
                .WithMatchDate(Clock.UtcNow.AddDays(i))
                .WithStatus(MatchStatus.Scheduled)
                .Build();
            matches.Add(match);
        }
        Context.Matches.AddRange(matches);
        await Context.SaveChangesAsync();

        var handler = new GetMatchesQueryHandler(Context);
        var query = new GetMatchesQuery(Page: 1, PageSize: 20);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(20);
        await Assert.That(result.Value.TotalCount).IsEqualTo(25);
        await Assert.That(result.Value.Page).IsEqualTo(1);
        await Assert.That(result.Value.TotalPages).IsEqualTo(2);
    }

    [Test]
    public async Task GetMatches_ByCompetition_ReturnsFilteredResults()
    {
        // Arrange
        var competition1 = new CompetitionBuilder().WithName("Premier League").Build();
        var competition2 = new CompetitionBuilder().WithName("La Liga").Build();
        Context.Competitions.AddRange(competition1, competition2);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);
        await Context.SaveChangesAsync();

        // Add 3 matches to competition 1
        for (int i = 0; i < 3; i++)
        {
            var match = new MatchBuilder()
                .WithCompetitionId(competition1.Id)
                .WithTeams(homeTeam.Id, awayTeam.Id)
                .WithMatchDate(Clock.UtcNow.AddDays(i))
                .Build();
            Context.Matches.Add(match);
        }

        // Add 5 matches to competition 2
        for (int i = 0; i < 5; i++)
        {
            var match = new MatchBuilder()
                .WithCompetitionId(competition2.Id)
                .WithTeams(homeTeam.Id, awayTeam.Id)
                .WithMatchDate(Clock.UtcNow.AddDays(i + 10))
                .Build();
            Context.Matches.Add(match);
        }
        await Context.SaveChangesAsync();

        var handler = new GetMatchesQueryHandler(Context);
        var query = new GetMatchesQuery(CompetitionId: competition1.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(3);
        await Assert.That(result.Value.TotalCount).IsEqualTo(3);
        await Assert.That(result.Value.Items.All(m => m.Competition.Id == competition1.Id)).IsTrue();
    }

    [Test]
    public async Task GetMatches_ByStatus_ReturnsFilteredResults()
    {
        // Arrange
        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);
        await Context.SaveChangesAsync();

        // Add scheduled matches
        for (int i = 0; i < 3; i++)
        {
            var match = new MatchBuilder()
                .WithCompetitionId(competition.Id)
                .WithTeams(homeTeam.Id, awayTeam.Id)
                .WithStatus(MatchStatus.Scheduled)
                .Build();
            Context.Matches.Add(match);
        }

        // Add finished matches
        for (int i = 0; i < 2; i++)
        {
            var match = new MatchBuilder()
                .WithCompetitionId(competition.Id)
                .WithTeams(homeTeam.Id, awayTeam.Id)
                .WithStatus(MatchStatus.Finished)
                .WithScore(2, 1)
                .Build();
            Context.Matches.Add(match);
        }
        await Context.SaveChangesAsync();

        var handler = new GetMatchesQueryHandler(Context);
        var query = new GetMatchesQuery(Status: MatchStatus.Finished);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(2);
        await Assert.That(result.Value.TotalCount).IsEqualTo(2);
        await Assert.That(result.Value.Items.All(m => m.Status == MatchStatus.Finished)).IsTrue();
    }
}
