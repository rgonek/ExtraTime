using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bets.Commands.DeleteBet;
using ExtraTime.Domain.Common;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Bets.Handlers;

[NotInParallel]
public sealed class DeleteBetCommandHandlerTests : HandlerTestBase
{
    private readonly DeleteBetCommandHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public DeleteBetCommandHandlerTests()
    {
        _handler = new DeleteBetCommandHandler(Context, CurrentUserService);
    }

    [Before(Test)]
    public void Setup()
    {
        Clock.Current = new FakeClock(_now);
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task Handle_BetNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        SetCurrentUser(userId);

        var mockBets = CreateMockDbSet(new List<Domain.Entities.Bet>().AsQueryable());
        Context.Bets.Returns(mockBets);

        var command = new DeleteBetCommand(leagueId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_NotBetOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var betId = Guid.NewGuid();
        
        var bet = new BetBuilder()
            .WithId(betId)
            .WithLeagueId(leagueId)
            .WithUserId(ownerId)
            .Build();

        SetCurrentUser(otherUserId);

        var bets = new List<Domain.Entities.Bet> { bet }.AsQueryable();
        var mockBets = CreateMockDbSet(bets);
        Context.Bets.Returns(mockBets);

        var command = new DeleteBetCommand(leagueId, betId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

}
