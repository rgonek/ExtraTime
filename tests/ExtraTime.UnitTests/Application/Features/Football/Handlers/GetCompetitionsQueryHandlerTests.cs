using ExtraTime.Application.Features.Football.DTOs;
using ExtraTime.Application.Features.Football.Queries.GetCompetitions;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Football.Handlers;

public sealed class GetCompetitionsQueryHandlerTests : HandlerTestBase
{
    private readonly GetCompetitionsQueryHandler _handler;

    public GetCompetitionsQueryHandlerTests()
    {
        _handler = new GetCompetitionsQueryHandler(Context);
    }

    [Test]
    public async Task Handle_NoCompetitions_ReturnsEmptyList()
    {
        // Arrange
        var mockCompetitions = CreateMockDbSet(new List<Domain.Entities.Competition>().AsQueryable());
        Context.Competitions.Returns(mockCompetitions);

        var query = new GetCompetitionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_CompetitionsExist_ReturnsOrderedByName()
    {
        // Arrange
        var competition1 = new CompetitionBuilder()
            .WithName("Premier League")
            .WithCode("PL")
            .Build();

        var competition2 = new CompetitionBuilder()
            .WithName("Champions League")
            .WithCode("CL")
            .Build();

        var competition3 = new CompetitionBuilder()
            .WithName("La Liga")
            .WithCode("LL")
            .Build();

        var competitions = new List<Domain.Entities.Competition>
        {
            competition1,
            competition2,
            competition3
        }.AsQueryable();

        var mockCompetitions = CreateMockDbSet(competitions);
        Context.Competitions.Returns(mockCompetitions);

        var query = new GetCompetitionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(3);

        // Verify ordering by name (Champions League, La Liga, Premier League)
        await Assert.That(result.Value![0].Name).IsEqualTo("Champions League");
        await Assert.That(result.Value![1].Name).IsEqualTo("La Liga");
        await Assert.That(result.Value![2].Name).IsEqualTo("Premier League");
    }

    [Test]
    public async Task Handle_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var competition = new CompetitionBuilder()
            .WithId(Guid.NewGuid())
            .WithExternalId(123)
            .WithName("Test League")
            .WithCode("TL")
            .Build();
        competition.UpdateDetails("Test League", "TL", "Test Country", "https://logo.url");
        competition.UpdateCurrentSeason(5, now.AddMonths(-3), now.AddMonths(6));

        var competitions = new List<Domain.Entities.Competition> { competition }.AsQueryable();
        var mockCompetitions = CreateMockDbSet(competitions);
        Context.Competitions.Returns(mockCompetitions);

        var query = new GetCompetitionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);

        var dto = result.Value![0];
        await Assert.That(dto.Id).IsEqualTo(competition.Id);
        await Assert.That(dto.ExternalId).IsEqualTo(competition.ExternalId);
        await Assert.That(dto.Name).IsEqualTo(competition.Name);
        await Assert.That(dto.Code).IsEqualTo(competition.Code);
        await Assert.That(dto.Country).IsEqualTo(competition.Country);
        await Assert.That(dto.LogoUrl).IsEqualTo(competition.LogoUrl);
        await Assert.That(dto.CurrentMatchday).IsEqualTo(competition.CurrentMatchday);
        await Assert.That(dto.CurrentSeasonStart).IsEqualTo(competition.CurrentSeasonStart);
        await Assert.That(dto.CurrentSeasonEnd).IsEqualTo(competition.CurrentSeasonEnd);
        await Assert.That(dto.LastSyncedAt).IsEqualTo(competition.LastSyncedAt);
    }
}
