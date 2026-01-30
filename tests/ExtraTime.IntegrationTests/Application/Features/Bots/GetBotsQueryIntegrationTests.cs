using ExtraTime.Application.Features.Bots.Queries.GetBots;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bots;

public sealed class GetBotsQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetBots_NoBots_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetBotsQueryHandler(Context);
        var query = new GetBotsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetBots_WithBots_ReturnsAllBots()
    {
        // Arrange
        var bot1 = new BotBuilder()
            .WithName("Bot 1")
            .WithStrategy(BotStrategy.Random)
            .WithIsActive(true)
            .Build();
        var bot2 = new BotBuilder()
            .WithName("Bot 2")
            .WithStrategy(BotStrategy.HomeFavorer)
            .WithIsActive(true)
            .Build();

        Context.Bots.AddRange(bot1, bot2);
        await Context.SaveChangesAsync();

        var handler = new GetBotsQueryHandler(Context);
        var query = new GetBotsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetBots_MultipleBots_ReturnsCorrectData()
    {
        // Arrange
        var bot1 = new BotBuilder()
            .WithName("RandomBot")
            .WithStrategy(BotStrategy.Random)
            .WithAvatarUrl("https://example.com/random.png")
            .WithIsActive(true)
            .Build();
        var bot2 = new BotBuilder()
            .WithName("HomeFavorerBot")
            .WithStrategy(BotStrategy.HomeFavorer)
            .WithAvatarUrl(null)
            .WithIsActive(false)
            .Build();
        var bot3 = new BotBuilder()
            .WithName("DrawPredictorBot")
            .WithStrategy(BotStrategy.DrawPredictor)
            .WithIsActive(true)
            .Build();

        Context.Bots.AddRange(bot1, bot2, bot3);
        await Context.SaveChangesAsync();

        var handler = new GetBotsQueryHandler(Context);
        var query = new GetBotsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(3);

        // Verify bot data
        var randomBot = result.Value.FirstOrDefault(b => b.Name == "RandomBot");
        await Assert.That(randomBot).IsNotNull();
        await Assert.That(randomBot!.Strategy).IsEqualTo("Random");
        await Assert.That(randomBot.IsActive).IsTrue();
        await Assert.That(randomBot.AvatarUrl).IsEqualTo("https://example.com/random.png");

        var homeFavorerBot = result.Value.FirstOrDefault(b => b.Name == "HomeFavorerBot");
        await Assert.That(homeFavorerBot).IsNotNull();
        await Assert.That(homeFavorerBot!.Strategy).IsEqualTo("HomeFavorer");
        await Assert.That(homeFavorerBot.IsActive).IsFalse();
        await Assert.That(homeFavorerBot.AvatarUrl).IsNull();

        var drawPredictorBot = result.Value.FirstOrDefault(b => b.Name == "DrawPredictorBot");
        await Assert.That(drawPredictorBot).IsNotNull();
        await Assert.That(drawPredictorBot!.Strategy).IsEqualTo("DrawPredictor");
    }
}
