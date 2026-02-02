using ExtraTime.Application.Features.Bots.Commands.RemoveBotFromLeague;
using ExtraTime.Application.Features.Bots.Commands.AddBotToLeague;
using ExtraTime.Application.Features.Bots.Commands.CreateBot;
using ExtraTime.Application.Features.Bots.Commands.PlaceBotBets;
using ExtraTime.Application.Features.Bots.Queries.GetBots;
using ExtraTime.Application.Features.Bots.Queries.GetLeagueBots;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.NewIntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.NewIntegrationTests.Tests.Bots;

public sealed class BotTests : NewIntegrationTestBase
{
    private async Task<(Bot bot, User user)> CreateBotWithUserAsync(string name = "TestBot", BotStrategy strategy = BotStrategy.Random)
    {
        var botUserId = Guid.NewGuid();
        var botUser = new UserBuilder()
            .WithId(botUserId)
            .WithEmail($"bot_{Guid.NewGuid()}@extratime.local")
            .WithUsername($"{name}_{Guid.NewGuid()}")
            .Build();
        
        var bot = new BotBuilder()
            .WithUserId(botUserId)
            .WithName(name)
            .WithStrategy(strategy)
            .Build();

        Context.Users.Add(botUser);
        Context.Bots.Add(bot);
        await Context.SaveChangesAsync();

        return (bot, botUser);
    }

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

        // Verify bot persisted to database
        var bot = await Context.Bots.FirstOrDefaultAsync(b => b.Name == "TestBot");
        await Assert.That(bot).IsNotNull();
        
        // Verify associated user created
        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == bot!.UserId);
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.IsBot).IsTrue();
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

        var botMember = await Context.LeagueBotMembers
            .FirstOrDefaultAsync(bm => bm.LeagueId == league.Id && bm.BotId == bot.Id);
        await Assert.That(botMember).IsNotNull();
    }

    [Test]
    public async Task RemoveBotFromLeague_ValidData_RemovesBotFromLeague()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder().WithOwnerId(ownerId).Build();
        Context.Leagues.Add(league);

        var (bot, _) = await CreateBotWithUserAsync("TestBot");

        var trackedLeague = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var botMemberAfter = await Context.LeagueBotMembers
            .FirstOrDefaultAsync(bm => bm.LeagueId == league.Id && bm.BotId == bot.Id);
        await Assert.That(botMemberAfter).IsNull();
    }

    [Test]
    public async Task GetBots_WithBots_ReturnsAllBots()
    {
        // Arrange
        await CreateBotWithUserAsync("Bot 1", BotStrategy.Random);
        await CreateBotWithUserAsync("Bot 2", BotStrategy.HomeFavorer);

        var handler = new GetBotsQueryHandler(Context);
        var query = new GetBotsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetLeagueBots_WithBots_ReturnsLeagueBots()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder().WithOwnerId(ownerId).Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var (bot1, _) = await CreateBotWithUserAsync("Bot 1");
        var (bot2, _) = await CreateBotWithUserAsync("Bot 2");

        var trackedLeague = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddBot(bot1.Id);
        trackedLeague.AddBot(bot2.Id);
        await Context.SaveChangesAsync();

        var handler = new GetLeagueBotsQueryHandler(Context);
        var query = new GetLeagueBotsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task PlaceBotBets_ReturnsSuccess()
    {
        // Arrange
        var botService = Substitute.For<IBotBettingService>();
        botService.PlaceBetsForUpcomingMatchesAsync(Arg.Any<CancellationToken>()).Returns(5);

        var handler = new PlaceBotBetsCommandHandler(botService);
        var command = new PlaceBotBetsCommand();

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(5);
    }
}
