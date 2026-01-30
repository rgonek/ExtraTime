using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.Commands.RecalculateLeagueStandings;
using ExtraTime.UnitTests.Common;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bets.Handlers;

public sealed class RecalculateLeagueStandingsCommandHandlerTests : HandlerTestBase
{
    private readonly RecalculateLeagueStandingsCommandHandler _handler;
    private readonly IStandingsCalculator _standingsCalculator;

    public RecalculateLeagueStandingsCommandHandlerTests()
    {
        _standingsCalculator = Substitute.For<IStandingsCalculator>();
        _handler = new RecalculateLeagueStandingsCommandHandler(_standingsCalculator);
    }

    [Test]
    public async Task Handle_SingleLeagueId_CallsRecalculateOnce()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var command = new RecalculateLeagueStandingsCommand([leagueId]);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _standingsCalculator.Received(1).RecalculateLeagueStandingsAsync(leagueId, CancellationToken);
    }

    [Test]
    public async Task Handle_MultipleLeagueIds_CallsRecalculateForEach()
    {
        // Arrange
        var leagueId1 = Guid.NewGuid();
        var leagueId2 = Guid.NewGuid();
        var leagueId3 = Guid.NewGuid();
        var command = new RecalculateLeagueStandingsCommand([leagueId1, leagueId2, leagueId3]);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _standingsCalculator.Received(1).RecalculateLeagueStandingsAsync(leagueId1, CancellationToken);
        await _standingsCalculator.Received(1).RecalculateLeagueStandingsAsync(leagueId2, CancellationToken);
        await _standingsCalculator.Received(1).RecalculateLeagueStandingsAsync(leagueId3, CancellationToken);
    }

    [Test]
    public async Task Handle_EmptyLeagueIdsArray_ReturnsSuccess()
    {
        // Arrange
        var command = new RecalculateLeagueStandingsCommand([]);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _standingsCalculator.DidNotReceive().RecalculateLeagueStandingsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_StandingsCalculatorThrowsException_PropagatesException()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var command = new RecalculateLeagueStandingsCommand([leagueId]);

        _standingsCalculator.RecalculateLeagueStandingsAsync(leagueId, CancellationToken)
            .Returns(Task.FromException(new InvalidOperationException("Calculation failed")));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _handler.Handle(command, CancellationToken);
        });
    }
}
