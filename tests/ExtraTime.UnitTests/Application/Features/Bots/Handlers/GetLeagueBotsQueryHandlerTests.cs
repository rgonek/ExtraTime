using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Application.Features.Bots.Queries.GetLeagueBots;
using ExtraTime.Domain.Entities;
using ExtraTime.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bots.Handlers;

public sealed class GetLeagueBotsQueryHandlerTests : HandlerTestBase
{
    private readonly GetLeagueBotsQueryHandler _handler;

    public GetLeagueBotsQueryHandlerTests()
    {
        _handler = new GetLeagueBotsQueryHandler(Context);
    }

    [Test]
    public async Task Handle_NoBotsInLeague_ReturnsEmptyList()
    {
        // Arrange
        var leagueId = Guid.NewGuid();

        var mockLeagueBotMembers = CreateMockDbSet(new List<LeagueBotMember>().AsQueryable());
        Context.LeagueBotMembers.Returns(mockLeagueBotMembers);

        var query = new GetLeagueBotsQuery(leagueId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }

    // Note: Tests with Include operations and navigation properties are better suited
    // for integration tests where a real database context can properly handle the
    // Include and Select projections. The handler logic is verified in integration tests.
}
