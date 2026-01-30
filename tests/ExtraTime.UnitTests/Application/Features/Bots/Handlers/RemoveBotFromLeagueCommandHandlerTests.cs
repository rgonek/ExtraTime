using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Commands.RemoveBotFromLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bots.Handlers;

public sealed class RemoveBotFromLeagueCommandHandlerTests : HandlerTestBase
{
    private readonly RemoveBotFromLeagueCommandHandler _handler;

    public RemoveBotFromLeagueCommandHandlerTests()
    {
        _handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
    }

    [Test]
    public async Task Handle_ValidCommand_RemovesBotFromLeague()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var botId = Guid.NewGuid();

        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        league.AddBot(botId);

        SetCurrentUser(ownerId);

        var leagues = new List<League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new RemoveBotFromLeagueCommand(leagueId, botId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var botId = Guid.NewGuid();

        SetCurrentUser(ownerId);

        var mockLeagues = CreateMockDbSet(new List<League>().AsQueryable());
        Context.Leagues.Returns(mockLeagues);

        var command = new RemoveBotFromLeagueCommand(Guid.NewGuid(), botId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("League not found");
    }

    [Test]
    public async Task Handle_NotLeagueOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var botId = Guid.NewGuid();

        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        SetCurrentUser(otherUserId);

        var leagues = new List<League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new RemoveBotFromLeagueCommand(leagueId, botId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("Only the league owner can remove bots");
    }

    [Test]
    public async Task Handle_BotNotInLeague_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var botId = Guid.NewGuid();

        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        // Don't add the bot to the league

        SetCurrentUser(ownerId);

        var leagues = new List<League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new RemoveBotFromLeagueCommand(leagueId, botId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("Bot is not in this league");
    }
}
