using ExtraTime.Application.Features.Bots.Queries.GetLeagueBots;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Bots;

public sealed class GetLeagueBotsQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetLeagueBots_NoBotsInLeague_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var handler = new GetLeagueBotsQueryHandler(Context);
        var query = new GetLeagueBotsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetLeagueBots_WithBots_ReturnsBots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var bot = new BotBuilder()
            .WithName("LeagueBot")
            .WithStrategy(BotStrategy.HomeFavorer)
            .Build();
        Context.Bots.Add(bot);
        await Context.SaveChangesAsync();

        var leagueBotMember = new LeagueBotMemberBuilder()
            .WithLeagueId(league.Id)
            .WithBotId(bot.Id)
            .Build();
        Context.LeagueBotMembers.Add(leagueBotMember);
        await Context.SaveChangesAsync();

        var handler = new GetLeagueBotsQueryHandler(Context);
        var query = new GetLeagueBotsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        await Assert.That(result.Value[0].Id).IsEqualTo(bot.Id);
        await Assert.That(result.Value[0].Name).IsEqualTo("LeagueBot");
        await Assert.That(result.Value[0].Strategy).IsEqualTo("HomeFavorer");
    }

    [Test]
    public async Task GetLeagueBots_LeagueNotFound_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetLeagueBotsQueryHandler(Context);
        var query = new GetLeagueBotsQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetLeagueBots_MultipleLeagues_ReturnsOnlyLeagueBots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league1 = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithName("League 1")
            .Build();
        var league2 = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithName("League 2")
            .Build();
        Context.Leagues.AddRange(league1, league2);
        await Context.SaveChangesAsync();

        // Add bot to league 1
        var bot1 = new BotBuilder()
            .WithName("BotForLeague1")
            .WithStrategy(BotStrategy.Random)
            .Build();
        Context.Bots.Add(bot1);
        await Context.SaveChangesAsync();

        var league1BotMember = new LeagueBotMemberBuilder()
            .WithLeagueId(league1.Id)
            .WithBotId(bot1.Id)
            .Build();
        Context.LeagueBotMembers.Add(league1BotMember);

        // Add two bots to league 2
        var bot2 = new BotBuilder()
            .WithName("Bot1ForLeague2")
            .WithStrategy(BotStrategy.HomeFavorer)
            .Build();
        var bot3 = new BotBuilder()
            .WithName("Bot2ForLeague2")
            .WithStrategy(BotStrategy.DrawPredictor)
            .Build();
        Context.Bots.AddRange(bot2, bot3);
        await Context.SaveChangesAsync();

        var league2BotMember1 = new LeagueBotMemberBuilder()
            .WithLeagueId(league2.Id)
            .WithBotId(bot2.Id)
            .Build();
        var league2BotMember2 = new LeagueBotMemberBuilder()
            .WithLeagueId(league2.Id)
            .WithBotId(bot3.Id)
            .Build();
        Context.LeagueBotMembers.AddRange(league2BotMember1, league2BotMember2);
        await Context.SaveChangesAsync();

        var handler = new GetLeagueBotsQueryHandler(Context);

        // Act - Query league 1
        var league1Result = await handler.Handle(new GetLeagueBotsQuery(league1.Id), default);

        // Assert - League 1 should have 1 bot
        await Assert.That(league1Result.IsSuccess).IsTrue();
        await Assert.That(league1Result.Value!.Count).IsEqualTo(1);
        await Assert.That(league1Result.Value[0].Name).IsEqualTo("BotForLeague1");

        // Act - Query league 2
        var league2Result = await handler.Handle(new GetLeagueBotsQuery(league2.Id), default);

        // Assert - League 2 should have 2 bots
        await Assert.That(league2Result.IsSuccess).IsTrue();
        await Assert.That(league2Result.Value!.Count).IsEqualTo(2);
    }
}
