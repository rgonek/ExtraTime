using ExtraTime.Application.Features.Bots.Commands.CreateBot;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bots;

[TestCategory(TestCategories.Significant)]
public sealed class CreateBotCommandIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task CreateBot_ValidData_CreatesBotAndUser()
    {
        // Arrange
        var passwordHasher = new PasswordHasher();
        var handler = new CreateBotCommandHandler(Context, passwordHasher);

        var command = new CreateBotCommand(
            "TestBot",
            "https://example.com/avatar.png",
            BotStrategy.Random,
            null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value.Name).IsEqualTo("TestBot");
        await Assert.That(result.Value.Strategy).IsEqualTo("Random");
        await Assert.That(result.Value.IsActive).IsTrue();

        // Verify bot persisted to database
        var bot = await Context.Bots
            .FirstOrDefaultAsync(b => b.Name == "TestBot");
        
        await Assert.That(bot).IsNotNull();
        await Assert.That(bot!.AvatarUrl).IsEqualTo("https://example.com/avatar.png");
        await Assert.That(bot.Strategy).IsEqualTo(BotStrategy.Random);

        // Verify associated user created
        var user = await Context.Users
            .FirstOrDefaultAsync(u => u.Id == bot.UserId);
        
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.IsBot).IsTrue();
        await Assert.That(user.Email).Contains("bot_testbot@extratime.local");
    }

    [Test]
    public async Task CreateBot_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var passwordHasher = new PasswordHasher();
        var handler = new CreateBotCommandHandler(Context, passwordHasher);

        // Create first bot
        var firstCommand = new CreateBotCommand(
            "DuplicateBot",
            null,
            BotStrategy.HomeFavorer,
            null);
        
        await handler.Handle(firstCommand, default);

        // Act - Try to create second bot with same name
        var secondCommand = new CreateBotCommand(
            "DuplicateBot",
            null,
            BotStrategy.Random,
            null);
        
        var result = await handler.Handle(secondCommand, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("already exists");
    }

    [Test]
    public async Task CreateBot_DifferentStrategies_CreatesSuccessfully()
    {
        // Arrange
        var passwordHasher = new PasswordHasher();
        var handler = new CreateBotCommandHandler(Context, passwordHasher);

        var strategies = new[]
        {
            BotStrategy.Random,
            BotStrategy.HomeFavorer,
            BotStrategy.DrawPredictor,
            BotStrategy.HighScorer,
            BotStrategy.UnderdogSupporter,
            BotStrategy.StatsAnalyst
        };

        foreach (var strategy in strategies)
        {
            // Act
            var command = new CreateBotCommand(
                $"Bot_{strategy}",
                null,
                strategy,
                strategy == BotStrategy.StatsAnalyst ? "{\"formWeight\":0.4}" : null);
            
            var result = await handler.Handle(command, default);

            // Assert
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.Strategy).IsEqualTo(strategy.ToString());
        }

        // Verify all bots created
        var botCount = await Context.Bots.CountAsync();
        await Assert.That(botCount).IsEqualTo(strategies.Length);
    }
}
