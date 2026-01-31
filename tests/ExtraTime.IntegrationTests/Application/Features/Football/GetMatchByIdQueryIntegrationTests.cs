using ExtraTime.Application.Features.Football;
using ExtraTime.Application.Features.Football.Queries.GetMatchById;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;

namespace ExtraTime.IntegrationTests.Application.Features.Football;

public sealed class GetMatchByIdQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetMatchById_ExistingMatch_ReturnsMatchDetails()
    {
        // Arrange
        var competition = new CompetitionBuilder()
            .WithName("Test Competition")
            .Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder()
            .WithName("Home Team")
            .WithShortName("HOME")
            .Build();
        var awayTeam = new TeamBuilder()
            .WithName("Away Team")
            .WithShortName("AWAY")
            .Build();
        Context.Teams.AddRange(homeTeam, awayTeam);
        await Context.SaveChangesAsync();

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(Clock.UtcNow.AddDays(1))
            .WithStatus(MatchStatus.Scheduled)
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
        await Assert.That(result.Value.Competition.Name).IsEqualTo("Test Competition");
        await Assert.That(result.Value.HomeTeam.Name).IsEqualTo("Home Team");
        await Assert.That(result.Value.AwayTeam.Name).IsEqualTo("Away Team");
        await Assert.That(result.Value.Status).IsEqualTo(MatchStatus.Scheduled);
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
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(FootballErrors.MatchNotFound);
    }
}
