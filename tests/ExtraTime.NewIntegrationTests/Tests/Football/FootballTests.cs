using ExtraTime.Application.Features.Football.Queries.GetCompetitions;
using ExtraTime.Application.Features.Football.Queries.GetMatches;
using ExtraTime.Application.Features.Football.Queries.GetMatchById;
using ExtraTime.Domain.Enums;
using ExtraTime.NewIntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.NewIntegrationTests.Tests.Football;

public sealed class FootballTests : NewIntegrationTestBase
{
    //
    // Competition Tests
    //

    [Test]
    public async Task GetCompetitions_WithCompetitions_ReturnsAllCompetitions()
    {
        // Arrange
        var competition1 = new CompetitionBuilder().WithName("Premier League").Build();
        var competition2 = new CompetitionBuilder().WithName("La Liga").Build();
        var competition3 = new CompetitionBuilder().WithName("Bundesliga").Build();

        Context.Competitions.AddRange(competition1, competition2, competition3);
        await Context.SaveChangesAsync();

        var handler = new GetCompetitionsQueryHandler(Context);
        var query = new GetCompetitionsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(3);

        var names = result.Value.Select(c => c.Name).ToList();
        await Assert.That(names).Contains("Premier League");
        await Assert.That(names).Contains("La Liga");
        await Assert.That(names).Contains("Bundesliga");
    }

    //
    // Match Tests
    //

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

        for (int i = 0; i < 25; i++)
        {
            var match = new MatchBuilder()
                .WithCompetitionId(competition.Id)
                .WithTeams(homeTeam.Id, awayTeam.Id)
                .WithMatchDate(DateTime.UtcNow.AddDays(i))
                .WithStatus(MatchStatus.Scheduled)
                .Build();
            Context.Matches.Add(match);
        }
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
    }

    [Test]
    public async Task GetMatchById_ExistingMatch_ReturnsMatch()
    {
        // Arrange
        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().WithName("Arsenal").Build();
        var awayTeam = new TeamBuilder().WithName("Chelsea").Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        var handler = new GetMatchByIdQueryHandler(Context);
        var query = new GetMatchByIdQuery(match.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Id).IsEqualTo(match.Id);
        await Assert.That(result.Value.HomeTeam.Name).IsEqualTo("Arsenal");
        await Assert.That(result.Value.AwayTeam.Name).IsEqualTo("Chelsea");
    }
}
