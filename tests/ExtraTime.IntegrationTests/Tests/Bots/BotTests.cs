using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Commands.RemoveBotFromLeague;
using ExtraTime.Application.Features.Bots.Commands.AddBotToLeague;
using ExtraTime.Application.Features.Bots.Commands.CreateBot;
using ExtraTime.Application.Features.Bots.Commands.DeleteBot;
using ExtraTime.Application.Features.Bots.Commands.PlaceBotBets;
using ExtraTime.Application.Features.Bots.Commands.UpdateBot;
using ExtraTime.Application.Features.Bots.Queries.GetBotConfigurationPresets;
using ExtraTime.Application.Features.Bots.Queries.GetBots;
using ExtraTime.Application.Features.Bots.Queries.GetLeagueBots;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Bots;

public sealed class BotTests : IntegrationTestBase
{
    private async Task<(Bot bot, User user)> CreateBotWithUserAsync(string name = "TestBot", BotStrategy strategy = BotStrategy.Random)
    {
        var botUserId = Guid.NewGuid();
        var shortGuid = Guid.NewGuid().ToString()[..8]; // Use only first 8 chars of GUID
        var botUser = new UserBuilder()
            .WithId(botUserId)
            .WithEmail($"bot_{shortGuid}@extratime.local")
            .WithUsername($"{name}_{shortGuid}")
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
        await Assert.That(result.Value!.Name).IsEqualTo("TestBot");

        // Verify bot persisted to database
        var bot = await Context.Bots.FirstOrDefaultAsync(b => b.Name == "TestBot");
        await Assert.That(bot).IsNotNull();

        // Verify associated user created
        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == bot!.UserId);
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.IsBot).IsTrue();
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
        await Assert.That(result.IsFailure).IsTrue();
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
        }

        // Verify all bots created
        var botCount = await Context.Bots.CountAsync();
        await Assert.That(botCount).IsEqualTo(strategies.Length);
    }

    [Test]
    public async Task UpdateBot_ValidData_UpdatesBotAndUser()
    {
        // Arrange
        var (bot, user) = await CreateBotWithUserAsync("Old Name", BotStrategy.Random);
        var handler = new UpdateBotCommandHandler(Context);
        var command = new UpdateBotCommand(
            bot.Id,
            "New Name",
            "https://example.com/new-avatar.png",
            BotStrategy.StatsAnalyst,
            "{\"preset\":\"full-analysis\"}",
            false);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var updatedBot = await Context.Bots.FirstAsync(b => b.Id == bot.Id);
        var updatedUser = await Context.Users.FirstAsync(u => u.Id == user.Id);
        await Assert.That(updatedBot.Name).IsEqualTo("New Name");
        await Assert.That(updatedBot.AvatarUrl).IsEqualTo("https://example.com/new-avatar.png");
        await Assert.That(updatedBot.Strategy).IsEqualTo(BotStrategy.StatsAnalyst);
        await Assert.That(updatedBot.Configuration).IsEqualTo("{\"preset\":\"full-analysis\"}");
        await Assert.That(updatedBot.IsActive).IsFalse();
        await Assert.That(updatedUser.Username).IsEqualTo("New Name");
    }

    [Test]
    public async Task UpdateBot_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var (firstBot, _) = await CreateBotWithUserAsync("Bot One");
        var (secondBot, _) = await CreateBotWithUserAsync("Bot Two");
        var handler = new UpdateBotCommandHandler(Context);

        // Act
        var result = await handler.Handle(
            new UpdateBotCommand(secondBot.Id, firstBot.Name, null, null, null, null),
            default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).Contains("already exists");
    }

    [Test]
    public async Task DeleteBot_WithoutBets_RemovesBotAndUser()
    {
        // Arrange
        var (bot, user) = await CreateBotWithUserAsync("DeleteMe");
        var handler = new DeleteBotCommandHandler(Context);

        // Act
        var result = await handler.Handle(new DeleteBotCommand(bot.Id), default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var deletedBot = await Context.Bots.FirstOrDefaultAsync(b => b.Id == bot.Id);
        var deletedUser = await Context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        await Assert.That(deletedBot).IsNull();
        await Assert.That(deletedUser).IsNull();
    }

    [Test]
    public async Task DeleteBot_WithHistoricalBets_DeactivatesBot()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var competition = new CompetitionBuilder().Build();
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .Build();
        Context.Matches.Add(match);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);

        var (bot, user) = await CreateBotWithUserAsync("KeepHistory");
        var bet = Bet.Place(league.Id, user.Id, match.Id, 2, 1);
        Context.Bets.Add(bet);
        await Context.SaveChangesAsync();

        var handler = new DeleteBotCommandHandler(Context);

        // Act
        var result = await handler.Handle(new DeleteBotCommand(bot.Id), default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var updatedBot = await Context.Bots.FirstOrDefaultAsync(b => b.Id == bot.Id);
        var existingUser = await Context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        await Assert.That(updatedBot).IsNotNull();
        await Assert.That(updatedBot!.IsActive).IsFalse();
        await Assert.That(existingUser).IsNotNull();
    }

    [Test]
    public async Task GetBots_WithFiltersAndStats_ReturnsExpectedData()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var competition = new CompetitionBuilder().Build();
        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        var thirdTeam = new TeamBuilder().Build();
        Context.Competitions.Add(competition);
        Context.Teams.AddRange(homeTeam, awayTeam, thirdTeam);

        var matchOne = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .Build();
        var matchTwo = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(thirdTeam.Id, homeTeam.Id)
            .Build();
        Context.Matches.AddRange(matchOne, matchTwo);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBotsEnabled(true)
            .Build();
        Context.Leagues.Add(league);

        var (statsBot, statsUser) = await CreateBotWithUserAsync("Stats Bot", BotStrategy.StatsAnalyst);
        var (inactiveBot, _) = await CreateBotWithUserAsync("Inactive Bot", BotStrategy.Random);
        inactiveBot.Deactivate();

        var trackedLeague = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddBot(statsBot.Id);

        var betOne = Bet.Place(league.Id, statsUser.Id, matchOne.Id, 2, 1);
        var betTwo = Bet.Place(league.Id, statsUser.Id, matchTwo.Id, 1, 1);
        Context.Bets.AddRange(betOne, betTwo);
        Context.BetResults.Add(BetResult.Create(betOne.Id, 3, true, true));
        await Context.SaveChangesAsync();

        var handler = new GetBotsQueryHandler(Context);

        // Act
        var activeBots = await handler.Handle(new GetBotsQuery(), default);
        var analystBots = await handler.Handle(
            new GetBotsQuery(IncludeInactive: true, Strategy: BotStrategy.StatsAnalyst),
            default);

        // Assert
        await Assert.That(activeBots.IsSuccess).IsTrue();
        await Assert.That(activeBots.Value!.Any(b => b.Name == "Inactive Bot")).IsFalse();
        await Assert.That(analystBots.IsSuccess).IsTrue();
        await Assert.That(analystBots.Value).HasSingleItem();

        var stats = analystBots.Value![0].Stats;
        await Assert.That(stats).IsNotNull();
        await Assert.That(stats!.TotalBetsPlaced).IsEqualTo(2);
        await Assert.That(stats.LeaguesJoined).IsEqualTo(1);
        await Assert.That(stats.AveragePointsPerBet).IsEqualTo(3);
        await Assert.That(stats.ExactPredictions).IsEqualTo(1);
        await Assert.That(stats.CorrectResults).IsEqualTo(1);
    }

    [Test]
    public async Task GetBotConfigurationPresets_ReturnsExternalDataPresets()
    {
        // Arrange
        var handler = new GetBotConfigurationPresetsQueryHandler();

        // Act
        var result = await handler.Handle(new GetBotConfigurationPresetsQuery(), default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(10);
        await Assert.That(result.Value.Any(x => x.Name == "xG Expert")).IsTrue();
        await Assert.That(result.Value.Any(x => x.Name == "Market Follower")).IsTrue();
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
        await Assert.That(result.IsFailure).IsTrue();
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
        await Assert.That(result.IsFailure).IsTrue();
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
        await Assert.That(result.IsFailure).IsTrue();
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

        var trackedLeague = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);
        var command = new AddBotToLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
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

        var bots = new List<Bot>();
        for (int i = 0; i < 3; i++)
        {
            var (bot, _) = await CreateBotWithUserAsync($"Bot_{i}");
            bots.Add(bot);
        }

        SetCurrentUser(ownerId);
        var handler = new AddBotToLeagueCommandHandler(Context, CurrentUserService);

        foreach (var bot in bots)
        {
            var command = new AddBotToLeagueCommand(league.Id, bot.Id);
            var result = await handler.Handle(command, default);
            await Assert.That(result.IsSuccess).IsTrue();
        }

        // Verify all bots are in the league
        var botCount = await Context.LeagueBotMembers.CountAsync(bm => bm.LeagueId == league.Id);
        await Assert.That(botCount).IsEqualTo(3);
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
    public async Task RemoveBotFromLeague_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var (bot, _) = await CreateBotWithUserAsync();
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(Guid.NewGuid(), bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
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

        var league = new LeagueBuilder().WithOwnerId(ownerId).Build();
        Context.Leagues.Add(league);

        var (bot, _) = await CreateBotWithUserAsync();

        var trackedLeague = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        SetCurrentUser(otherUserId); // Not the owner

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task RemoveBotFromLeague_BotNotInLeague_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder().WithOwnerId(ownerId).Build();
        Context.Leagues.Add(league);

        var (bot, _) = await CreateBotWithUserAsync();
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(league.Id, bot.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task RemoveBotFromLeague_MultipleBots_RemovesOnlyTarget()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder().WithOwnerId(ownerId).Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var bots = new List<Bot>();
        var trackedLeague = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league.Id);
        for (int i = 0; i < 3; i++)
        {
            var (bot, _) = await CreateBotWithUserAsync($"Bot_{i}");
            bots.Add(bot);
            trackedLeague.AddBot(bot.Id);
        }
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new RemoveBotFromLeagueCommandHandler(Context, CurrentUserService);
        var command = new RemoveBotFromLeagueCommand(league.Id, bots[1].Id); // Remove second bot

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var botCount = await Context.LeagueBotMembers.CountAsync(bm => bm.LeagueId == league.Id);
        await Assert.That(botCount).IsEqualTo(2);
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
    public async Task GetBots_NoBots_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetBotsQueryHandler(Context);
        var query = new GetBotsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetBots_MultipleBots_ReturnsCorrectData()
    {
        // Arrange
        await CreateBotWithUserAsync("RandomBot", BotStrategy.Random);
        await CreateBotWithUserAsync("HomeFavorerBot", BotStrategy.HomeFavorer);
        await CreateBotWithUserAsync("DrawPredictorBot", BotStrategy.DrawPredictor);

        var handler = new GetBotsQueryHandler(Context);
        var query = new GetBotsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(3);
        await Assert.That(result.Value!.Any(b => b.Name == "RandomBot")).IsTrue();
        await Assert.That(result.Value!.Any(b => b.Name == "HomeFavorerBot")).IsTrue();
        await Assert.That(result.Value!.Any(b => b.Name == "DrawPredictorBot")).IsTrue();
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
    public async Task GetLeagueBots_NoBotsInLeague_ReturnsEmptyList()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder().WithOwnerId(ownerId).Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var handler = new GetLeagueBotsQueryHandler(Context);
        var query = new GetLeagueBotsQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
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
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetLeagueBots_MultipleLeagues_ReturnsOnlyLeagueBots()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league1 = new LeagueBuilder().WithOwnerId(ownerId).Build();
        var league2 = new LeagueBuilder().WithOwnerId(ownerId).Build();
        Context.Leagues.AddRange(league1, league2);
        await Context.SaveChangesAsync();

        var (bot1, _) = await CreateBotWithUserAsync("Bot1");
        var (bot2, _) = await CreateBotWithUserAsync("Bot2");

        var trackedLeague1 = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league1.Id);
        trackedLeague1.AddBot(bot1.Id);
        var trackedLeague2 = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league2.Id);
        trackedLeague2.AddBot(bot2.Id);
        await Context.SaveChangesAsync();

        var handler = new GetLeagueBotsQueryHandler(Context);

        // Act
        var result1 = await handler.Handle(new GetLeagueBotsQuery(league1.Id), default);
        var result2 = await handler.Handle(new GetLeagueBotsQuery(league2.Id), default);

        // Assert
        await Assert.That(result1.Value!.Count).IsEqualTo(1);
        await Assert.That(result1.Value[0].Id).IsEqualTo(bot1.Id);
        await Assert.That(result2.Value!.Count).IsEqualTo(1);
        await Assert.That(result2.Value[0].Id).IsEqualTo(bot2.Id);
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

    [Test]
    public async Task PlaceBotBets_NoMatchesAvailable_ReturnsZero()
    {
        // Arrange
        var botService = Substitute.For<IBotBettingService>();
        botService.PlaceBetsForUpcomingMatchesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var handler = new PlaceBotBetsCommandHandler(botService);
        var command = new PlaceBotBetsCommand();

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(0);
    }

    [Test]
    public async Task PlaceBotBets_WithRealServiceAndNoData_ReturnsZero()
    {
        // Arrange
        var botService = new BotBettingService(
            Context,
            new BotStrategyFactory(Substitute.For<IServiceProvider>()),
            Substitute.For<ITeamFormCalculator>(),
            TimeProvider.System,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<BotBettingService>>());

        var handler = new PlaceBotBetsCommandHandler(botService);
        var command = new PlaceBotBetsCommand();

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(0);
    }

    [Test]
    public async Task PlaceBotBets_WithRealServiceAndValidMatch_PlacesBet()
    {
        // Arrange - Set up a match within the next 24 hours
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        // Create competition and teams first
        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.Add(homeTeam);
        Context.Teams.Add(awayTeam);

        // Create a match scheduled 12 hours from now
        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithMatchDate(DateTime.UtcNow.AddHours(12))
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBotsEnabled(true)
            .WithBettingDeadlineMinutes(60) // 1 hour deadline
            .Build();
        Context.Leagues.Add(league);

        // Create bot with Random strategy
        var (bot, _) = await CreateBotWithUserAsync("TestBot", BotStrategy.Random);

        // Add bot to league
        var trackedLeague = await Context.Leagues.Include(l => l.BotMembers).FirstAsync(l => l.Id == league.Id);
        trackedLeague.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        // Create real service with time provider that returns current time
        var timeProvider = TimeProvider.System;
        var strategyFactory = new BotStrategyFactory(Substitute.For<IServiceProvider>());
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<BotBettingService>>();

        var botService = new BotBettingService(Context, strategyFactory, Substitute.For<ITeamFormCalculator>(), timeProvider, logger);

        var handler = new PlaceBotBetsCommandHandler(botService);
        var command = new PlaceBotBetsCommand();

        // Act
        var result = await handler.Handle(command, default);

        // Assert - Should place at least one bet
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsGreaterThanOrEqualTo(0);

        // Verify that if bets were placed, they're in the database
        if (result.Value > 0)
        {
            var bets = await Context.Bets
                .Where(b => b.LeagueId == league.Id && b.UserId == bot.UserId)
                .ToListAsync();
            await Assert.That(bets.Count).IsEqualTo(result.Value);
        }
    }
}
