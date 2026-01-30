using ExtraTime.Application.Features.Football.Queries.GetCompetitions;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Football;

public sealed class GetCompetitionsQueryIntegrationTests : IntegrationTestBase
{
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
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetCompetitions_WithCompetitions_ReturnsAllCompetitions()
    {
        // Arrange
        var competition1 = new CompetitionBuilder()
            .WithName("Premier League")
            .Build();
        var competition2 = new CompetitionBuilder()
            .WithName("La Liga")
            .Build();
        var competition3 = new CompetitionBuilder()
            .WithName("Bundesliga")
            .Build();

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

        // Verify all competitions are returned
        var names = result.Value.Select(c => c.Name).ToList();
        await Assert.That(names).Contains("Premier League");
        await Assert.That(names).Contains("La Liga");
        await Assert.That(names).Contains("Bundesliga");
    }

    [Test]
    public async Task GetCompetitions_ReturnsOrderedByName()
    {
        // Arrange
        // Add competitions in non-alphabetical order
        var competition1 = new CompetitionBuilder()
            .WithName("Z League")
            .Build();
        var competition2 = new CompetitionBuilder()
            .WithName("A League")
            .Build();
        var competition3 = new CompetitionBuilder()
            .WithName("M League")
            .Build();

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

        // Verify competitions are ordered alphabetically by name
        await Assert.That(result.Value[0].Name).IsEqualTo("A League");
        await Assert.That(result.Value[1].Name).IsEqualTo("M League");
        await Assert.That(result.Value[2].Name).IsEqualTo("Z League");
    }
}
