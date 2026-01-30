using ExtraTime.Application.Features.Bots.Commands.PlaceBotBets;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.UnitTests.Common;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bots.Handlers;

public sealed class PlaceBotBetsCommandHandlerTests : HandlerTestBase
{
    private readonly IBotBettingService _botBettingService;
    private readonly PlaceBotBetsCommandHandler _handler;

    public PlaceBotBetsCommandHandlerTests()
    {
        _botBettingService = Substitute.For<IBotBettingService>();
        _handler = new PlaceBotBetsCommandHandler(_botBettingService);
    }

    [Test]
    public async Task Handle_ServicePlacesBets_ReturnsSuccessWithCount()
    {
        // Arrange
        var expectedCount = 5;
        _botBettingService.PlaceBetsForUpcomingMatchesAsync(CancellationToken)
            .Returns(expectedCount);

        var command = new PlaceBotBetsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(expectedCount);
    }

    [Test]
    public async Task Handle_NoMatchesToBet_ReturnsSuccessWithZero()
    {
        // Arrange
        _botBettingService.PlaceBetsForUpcomingMatchesAsync(CancellationToken)
            .Returns(0);

        var command = new PlaceBotBetsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _botBettingService.PlaceBetsForUpcomingMatchesAsync(CancellationToken)
            .Returns(Task.FromException<int>(new InvalidOperationException("Service error")));

        var command = new PlaceBotBetsCommand();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, CancellationToken));
    }
}
