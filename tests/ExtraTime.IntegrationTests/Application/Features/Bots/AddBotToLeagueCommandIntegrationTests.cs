using ExtraTime.Application.Features.Bots.Commands.AddBotToLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

using ExtraTime.IntegrationTests.Attributes;

namespace ExtraTime.IntegrationTests.Application.Features.Bots;

[TestCategory(TestCategories.RequiresDatabase)]
public sealed class AddBotToLeagueCommandIntegrationTests : IntegrationTestBase
{
    protected override bool UseSqlDatabase => true;

    private async Task<(Bot bot, User user)> CreateBotWithUserAsync(string name = "TestBot", BotStrategy strategy = BotStrategy.Random)
    {
        var bot = new BotBuilder()
            .WithName(name)
            .WithStrategy(strategy)
            .Build();

        var botUser = new UserBuilder()
            .WithId(bot.UserId)
            .WithEmail($"bot_{Guid.NewGuid()}@extratime.local")
            .WithUsername($"{name}_{Guid.NewGuid()}")
            .Build();
        
        Context.Users.Add(botUser);
        await Context.SaveChangesAsync();

        Context.Bots.Add(bot);
        await Context.SaveChangesAsync();

        return (bot, botUser);
    }

    [Test]
    public async Task AddBotToLeague_ValidData_AddsBotToLeague()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBotsEnabled(true)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var (bot, _) = await CreateBotWithUserAsync("TestBot");

        SetCurrentUser(ownerId);

        var handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);
        var command = new AddBotToLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value.Name).IsEqualTo("TestBot");

        // Verify bot membership persisted to database
        var botMember = await Context.LeagueBotMembers
            .FirstOrDefaultAsync(bm => bm.LeagueId == league.Id && bm.BotId == bot.Id);

        await Assert.That(botMember).IsNotNull();
    }

    [Test]
    public async Task AddBotToLeague_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var (bot, _) = await CreateBotWithUserAsync();

        SetCurrentUser(ownerId);

        var handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);
        var command = new AddBotToLeagueCommand(Guid.NewGuid(), bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("League not found");
    }

    [Test]
    public async Task AddBotToLeague_BotNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);
        var command = new AddBotToLeagueCommand(league.Id, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("Bot not found");
    }

    [Test]
    public async Task AddBotToLeague_NotOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();
        Context.Users.Add(otherUser);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var (bot, _) = await CreateBotWithUserAsync();

        SetCurrentUser(otherUserId); // Not the owner

        var handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);
        var command = new AddBotToLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("Only the league owner");
    }

    [Test]
    public async Task AddBotToLeague_BotAlreadyInLeague_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var (bot, _) = await CreateBotWithUserAsync();

        // Add bot to league first
        league.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);
        var command = new AddBotToLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("already in this league");
    }

    [Test]
    public async Task AddBotToLeague_MultipleBots_AddsSuccessfully()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBotsEnabled(true)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var strategies = new[] { BotStrategy.Random, BotStrategy.HomeFavorer, BotStrategy.DrawPredictor };
        var bots = new List<Bot>();

        foreach (var strategy in strategies)
        {
            var (bot, _) = await CreateBotWithUserAsync($"Bot_{strategy}", strategy);
            bots.Add(bot);
        }

        SetCurrentUser(ownerId);

        // Act - Add all bots
        var handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);
        var results = new List<bool>();

        foreach (var bot in bots)
        {
            var command = new AddBotToLeagueCommand(league.Id, bot.Id);
            var result = await handler.Handle(command, default);
            results.Add(result.IsSuccess);
        }

        // Assert
        await Assert.That(results.All(r => r)).IsTrue();

        // Verify all bots are in the league
        var botCount = await Context.LeagueBotMembers
            .CountAsync(bm => bm.LeagueId == league.Id);

        await Assert.That(botCount).IsEqualTo(strategies.Length);
    }
}
