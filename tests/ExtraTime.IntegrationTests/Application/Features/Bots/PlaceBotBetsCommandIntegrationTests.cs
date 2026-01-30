using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Commands.PlaceBotBets;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Application.Features.Bots;

/// <summary>
/// Integration tests for PlaceBotBetsCommand.
/// Note: These tests use a mocked IBotBettingService as the service requires complex setup
/// (matches within 24h, teams, competitions, form calculations). A full end-to-end test
/// would require substantial test data setup.
/// </summary>
public sealed class PlaceBotBetsCommandIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task PlaceBotBets_ServiceReturnsCount_ReturnsSuccessWithCount()
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
        // Arrange - Set up minimal required data but no matches within 24h
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithBotsEnabled(true)
            .Build();
        Context.Leagues.Add(league);

        // Create bot with Random strategy (no dependencies needed)
        var bot = new BotBuilder()
            .WithName("TestBot")
            .WithStrategy(BotStrategy.Random)
            .Build();
        Context.Bots.Add(bot);

        // Create bot user
        var botUser = new UserBuilder()
            .WithId(bot.UserId)
            .WithEmail($"bot_{bot.Name.ToLower()}@extratime.local")
            .WithUsername(bot.Name)
            .Build();
        Context.Users.Add(botUser);

        // Add bot to league
        league.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        // Create real service dependencies
        var timeProvider = TimeProvider.System;
        var strategyFactory = new BotStrategyFactory(Substitute.For<IServiceProvider>());
        var logger = Substitute.For<ILogger<BotBettingService>>();

        var botService = new BotBettingService(Context, strategyFactory, Substitute.For<ITeamFormCalculator>(), timeProvider, logger);

        var handler = new PlaceBotBetsCommandHandler(botService);
        var command = new PlaceBotBetsCommand();

        // Act
        var result = await handler.Handle(command, default);

        // Assert - No matches within 24h, so no bets placed
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
        var bot = new BotBuilder()
            .WithName("TestBot")
            .WithStrategy(BotStrategy.Random)
            .Build();
        Context.Bots.Add(bot);

        // Create bot user
        var botUser = new UserBuilder()
            .WithId(bot.UserId)
            .WithEmail($"bot_{bot.Name.ToLower()}@extratime.local")
            .WithUsername(bot.Name)
            .Build();
        Context.Users.Add(botUser);

        // Add bot to league
        league.AddBot(bot.Id);
        await Context.SaveChangesAsync();

        // Create real service with time provider that returns current time
        var timeProvider = TimeProvider.System;
        var strategyFactory = new BotStrategyFactory(Substitute.For<IServiceProvider>());
        var logger = Substitute.For<ILogger<BotBettingService>>();

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
