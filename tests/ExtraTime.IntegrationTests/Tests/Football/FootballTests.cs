using ExtraTime.Application.Features.Football;
using ExtraTime.Application.Features.Football.Queries.GetCompetitions;
using ExtraTime.Application.Features.Football.Queries.GetMatches;
using ExtraTime.Application.Features.Football.Queries.GetMatchById;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Tests.Football;

public sealed class FootballTests : IntegrationTestBase
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

    [Test]
    public async Task GetCompetitions_NoCompetitions_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetCompetitionsQueryHandler(Context);
        var query = new GetCompetitionsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetCompetitions_ReturnsOrderedByName()
    {
        // Arrange
        var competition1 = new CompetitionBuilder().WithName("Z League").Build();
        var competition2 = new CompetitionBuilder().WithName("A League").Build();
        var competition3 = new CompetitionBuilder().WithName("M League").Build();

        Context.Competitions.AddRange(competition1, competition2, competition3);
        await Context.SaveChangesAsync();

        var handler = new GetCompetitionsQueryHandler(Context);
        var query = new GetCompetitionsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.Value[0].Name).IsEqualTo("A League");
        await Assert.That(result.Value[1].Name).IsEqualTo("M League");
        await Assert.That(result.Value[2].Name).IsEqualTo("Z League");
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
    public async Task GetMatches_NoMatches_ReturnsEmptyPage()
    {
        // Arrange
        var handler = new GetMatchesQueryHandler(Context);
        var query = new GetMatchesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(0);
        await Assert.That(result.Value.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetMatches_ByCompetition_ReturnsFilteredResults()
    {
        // Arrange
        var competition1 = new CompetitionBuilder().WithName("Comp 1").Build();
        var competition2 = new CompetitionBuilder().WithName("Comp 2").Build();
        Context.Competitions.AddRange(competition1, competition2);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);
        await Context.SaveChangesAsync();

        var match1 = new MatchBuilder().WithCompetitionId(competition1.Id).WithTeams(homeTeam.Id, awayTeam.Id).Build();
        var match2 = new MatchBuilder().WithCompetitionId(competition2.Id).WithTeams(homeTeam.Id, awayTeam.Id).Build();
        Context.Matches.AddRange(match1, match2);
        await Context.SaveChangesAsync();

        var handler = new GetMatchesQueryHandler(Context);
        var query = new GetMatchesQuery(CompetitionId: competition1.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.Value!.Items.Count).IsEqualTo(1);
        await Assert.That(result.Value.Items[0].Id).IsEqualTo(match1.Id);
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

        var match1 = new MatchBuilder().WithCompetitionId(competition.Id).WithTeams(homeTeam.Id, awayTeam.Id).WithStatus(MatchStatus.Scheduled).Build();
        var match2 = new MatchBuilder().WithCompetitionId(competition.Id).WithTeams(homeTeam.Id, awayTeam.Id).WithStatus(MatchStatus.Finished).WithScore(1, 1).Build();
        Context.Matches.AddRange(match1, match2);
        await Context.SaveChangesAsync();

        var handler = new GetMatchesQueryHandler(Context);
        var query = new GetMatchesQuery(Status: MatchStatus.Finished);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.Value!.Items.Count).IsEqualTo(1);
        await Assert.That(result.Value.Items[0].Id).IsEqualTo(match2.Id);
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

    [Test]
    public async Task GetMatchById_MatchNotFound_ReturnsFailure()
    {
        // Arrange
        var handler = new GetMatchByIdQueryHandler(Context);
        var query = new GetMatchByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(FootballErrors.MatchNotFound);
    }
}
