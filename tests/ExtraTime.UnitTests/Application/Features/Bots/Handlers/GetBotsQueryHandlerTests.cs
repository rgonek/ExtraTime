using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Application.Features.Bots.Queries.GetBots;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bots.Handlers;

public sealed class GetBotsQueryHandlerTests : HandlerTestBase
{
    private readonly GetBotsQueryHandler _handler;

    public GetBotsQueryHandlerTests()
    {
        _handler = new GetBotsQueryHandler(Context);
    }

    [Test]
    public async Task Handle_NoBots_ReturnsEmptyList()
    {
        // Arrange
        var mockBots = CreateMockDbSet(new List<Bot>().AsQueryable());
        Context.Bots.Returns(mockBots);

        var query = new GetBotsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }

    [Test]
    public async Task Handle_WithBots_ReturnsAllBots()
    {
        // Arrange
        var bots = new List<Bot>
        {
            new BotBuilder().WithId(Guid.NewGuid()).WithName("Bot1").WithStrategy(BotStrategy.Random).Build(),
            new BotBuilder().WithId(Guid.NewGuid()).WithName("Bot2").WithStrategy(BotStrategy.HomeFavorer).Build(),
            new BotBuilder().WithId(Guid.NewGuid()).WithName("Bot3").WithStrategy(BotStrategy.StatsAnalyst).Build()
        };

        var mockBots = CreateMockDbSet(bots.AsQueryable());
        Context.Bots.Returns(mockBots);

        var query = new GetBotsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(3);
        await Assert.That(result.Value[0].Name).IsEqualTo("Bot1");
        await Assert.That(result.Value[1].Name).IsEqualTo("Bot2");
        await Assert.That(result.Value[2].Name).IsEqualTo("Bot3");
    }

    [Test]
    public async Task Handle_BotsWithDifferentStrategies_ReturnsCorrectStrategyNames()
    {
        // Arrange
        var bots = new List<Bot>
        {
            new BotBuilder().WithName("RandomBot").WithStrategy(BotStrategy.Random).Build(),
            new BotBuilder().WithName("HomeBot").WithStrategy(BotStrategy.HomeFavorer).Build(),
            new BotBuilder().WithName("UnderdogBot").WithStrategy(BotStrategy.UnderdogSupporter).Build(),
            new BotBuilder().WithName("DrawBot").WithStrategy(BotStrategy.DrawPredictor).Build(),
            new BotBuilder().WithName("ScorerBot").WithStrategy(BotStrategy.HighScorer).Build(),
            new BotBuilder().WithName("AnalystBot").WithStrategy(BotStrategy.StatsAnalyst).Build()
        };

        var mockBots = CreateMockDbSet(bots.AsQueryable());
        Context.Bots.Returns(mockBots);

        var query = new GetBotsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(6);

        foreach (var botDto in result.Value)
        {
            var originalBot = bots.First(b => b.Name == botDto.Name);
            await Assert.That(botDto.Strategy).IsEqualTo(originalBot.Strategy.ToString());
        }
    }

    [Test]
    public async Task Handle_BotsWithAvatarUrls_ReturnsAvatarUrls()
    {
        // Arrange
        var bots = new List<Bot>
        {
            new BotBuilder()
                .WithName("BotWithAvatar")
                .WithAvatarUrl("https://example.com/avatar.png")
                .Build(),
            new BotBuilder()
                .WithName("BotWithoutAvatar")
                .WithAvatarUrl(null)
                .Build()
        };

        var mockBots = CreateMockDbSet(bots.AsQueryable());
        Context.Bots.Returns(mockBots);

        var query = new GetBotsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
        await Assert.That(result.Value[0].AvatarUrl).IsEqualTo("https://example.com/avatar.png");
        await Assert.That(result.Value[1].AvatarUrl).IsNull();
    }

    [Test]
    public async Task Handle_BotsWithLastBetPlacedAt_ReturnsCorrectDates()
    {
        // Arrange
        var lastBetDate = DateTime.UtcNow.AddDays(-1);
        var bots = new List<Bot>
        {
            new BotBuilder()
                .WithName("ActiveBot")
                .WithLastBetPlacedAt(lastBetDate)
                .Build(),
            new BotBuilder()
                .WithName("InactiveBot")
                .WithLastBetPlacedAt(null)
                .Build()
        };

        var mockBots = CreateMockDbSet(bots.AsQueryable());
        Context.Bots.Returns(mockBots);

        var query = new GetBotsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
        await Assert.That(result.Value[0].LastBetPlacedAt).IsEqualTo(lastBetDate);
        await Assert.That(result.Value[1].LastBetPlacedAt).IsNull();
    }
}
