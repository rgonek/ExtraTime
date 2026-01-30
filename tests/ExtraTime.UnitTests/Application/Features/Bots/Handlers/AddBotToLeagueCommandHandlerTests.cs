using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Commands.AddBotToLeague;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bots.Handlers;

public sealed class AddBotToLeagueCommandHandlerTests : HandlerTestBase
{
    private readonly AddBotToLeagueCommandHandler _handler;

    public AddBotToLeagueCommandHandlerTests()
    {
        _handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);
    }

    [Test]
    public async Task Handle_ValidCommand_AddsBotToLeague()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var botId = Guid.NewGuid();

        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        var bot = new BotBuilder()
            .WithId(botId)
            .WithName("TestBot")
            .Build();

        SetCurrentUser(ownerId);

        var leagues = new List<League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var bots = new List<Bot> { bot }.AsQueryable();
        var mockBots = CreateMockDbSet(bots);
        Context.Bots.Returns(mockBots);

        var command = new AddBotToLeagueCommand(leagueId, botId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Name).IsEqualTo("TestBot");
        await Assert.That(result.Value.Id).IsEqualTo(botId);

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

        var command = new AddBotToLeagueCommand(Guid.NewGuid(), botId);

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

        var command = new AddBotToLeagueCommand(leagueId, botId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("Only the league owner can add bots");
    }

    [Test]
    public async Task Handle_BotNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var botId = Guid.NewGuid();

        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        SetCurrentUser(ownerId);

        var leagues = new List<League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var mockBots = CreateMockDbSet(new List<Bot>().AsQueryable());
        Context.Bots.Returns(mockBots);

        var command = new AddBotToLeagueCommand(leagueId, botId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("Bot not found");
    }

    [Test]
    public async Task Handle_BotAlreadyInLeague_ReturnsFailure()
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

        var bot = new BotBuilder()
            .WithId(botId)
            .WithName("TestBot")
            .Build();

        SetCurrentUser(ownerId);

        var leagues = new List<League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var bots = new List<Bot> { bot }.AsQueryable();
        var mockBots = CreateMockDbSet(bots);
        Context.Bots.Returns(mockBots);

        var command = new AddBotToLeagueCommand(leagueId, botId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("Bot is already in this league");
    }
}
