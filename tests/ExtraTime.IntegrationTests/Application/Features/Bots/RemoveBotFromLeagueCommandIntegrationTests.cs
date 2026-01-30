using ExtraTime.Application.Features.Bots.Commands.RemoveBotFromLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bots;

public sealed class RemoveBotFromLeagueCommandIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task RemoveBotFromLeague_ValidData_RemovesBotFromLeague()
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

        var bot = new BotBuilder()
            .WithName("TestBot")
            .Build();
        Context.Bots.Add(bot);

        // Add bot to league first
        league.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        // Verify bot is in the league
        var botMemberBefore = await Context.LeagueBotMembers
            .FirstOrDefaultAsync(bm => bm.LeagueId == league.Id && bm.BotId == bot.Id);
        await Assert.That(botMemberBefore).IsNotNull();

        SetCurrentUser(ownerId);

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        // Verify bot membership removed from database
        var botMemberAfter = await Context.LeagueBotMembers
            .FirstOrDefaultAsync(bm => bm.LeagueId == league.Id && bm.BotId == bot.Id);
        await Assert.That(botMemberAfter).IsNull();
    }

    [Test]
    public async Task RemoveBotFromLeague_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var bot = new BotBuilder().Build();
        Context.Bots.Add(bot);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(Guid.NewGuid(), bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("League not found");
    }

    [Test]
    public async Task RemoveBotFromLeague_NotOwner_ReturnsFailure()
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

        var bot = new BotBuilder().Build();
        Context.Bots.Add(bot);

        league.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        SetCurrentUser(otherUserId); // Not the owner

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("Only the league owner");
    }

    [Test]
    public async Task RemoveBotFromLeague_BotNotInLeague_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);

        var bot = new BotBuilder().Build();
        Context.Bots.Add(bot);

        // Bot is NOT added to league
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("Bot is not in this league");
    }

    [Test]
    public async Task RemoveBotFromLeague_MultipleBots_RemovesOnlyTarget()
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

        // Add 3 bots
        var bots = new List<Bot>();
        for (int i = 1; i <= 3; i++)
        {
            var bot = new BotBuilder()
                .WithName($"Bot{i}")
                .Build();
            Context.Bots.Add(bot);
            bots.Add(bot);
            league.AddBot(bot.Id);
        }
        await Context.SaveChangesAsync();

        // Verify all bots are in the league
        var botCountBefore = await Context.LeagueBotMembers
            .CountAsync(bm => bm.LeagueId == league.Id);
        await Assert.That(botCountBefore).IsEqualTo(3);

        SetCurrentUser(ownerId);

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(league.Id, bots[1].Id); // Remove second bot

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        // Verify only 2 bots remain in the league
        var botCountAfter = await Context.LeagueBotMembers
            .CountAsync(bm => bm.LeagueId == league.Id);
        await Assert.That(botCountAfter).IsEqualTo(2);

        // Verify the removed bot is not in the league anymore
        var removedBot = await Context.LeagueBotMembers
            .FirstOrDefaultAsync(bm => bm.LeagueId == league.Id && bm.BotId == bots[1].Id);
        await Assert.That(removedBot).IsNull();

        // Verify other bots are still in the league
        var remainingBot1 = await Context.LeagueBotMembers
            .FirstOrDefaultAsync(bm => bm.LeagueId == league.Id && bm.BotId == bots[0].Id);
        var remainingBot2 = await Context.LeagueBotMembers
            .FirstOrDefaultAsync(bm => bm.LeagueId == league.Id && bm.BotId == bots[2].Id);
        await Assert.That(remainingBot1).IsNotNull();
        await Assert.That(remainingBot2).IsNotNull();
    }
}
