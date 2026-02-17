using ExtraTime.Application;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks)
builder.AddServiceDefaults();

// Add application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Override ICurrentUserService for background context (no HTTP request)
builder.Services.AddSingleton<ICurrentUserService, BackgroundUserService>();

// Build the host
using var host = builder.Build();
await host.StartAsync();

// Get the command from args
var command = args.Length > 0 ? args[0] : "";

// Get services
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var scope = host.Services.CreateScope();

try
{
    logger.LogInformation("==============================================");
    logger.LogInformation("ExtraTime Dev Trigger: {Command}", command);
    logger.LogInformation("==============================================");
    logger.LogInformation("");

    switch (command)
    {
        case "sync-competitions":
            await RunSyncCompetitionsAsync(scope.ServiceProvider, logger);
            break;

        case "sync-teams":
            await RunSyncTeamsAsync(scope.ServiceProvider, logger);
            break;

        case "sync-matches":
            await RunSyncMatchesAsync(scope.ServiceProvider, logger);
            break;

        case "sync-standings":
            await RunSyncStandingsAsync(scope.ServiceProvider, logger);
            break;

        case "sync-all":
            await RunSyncAllAsync(scope.ServiceProvider, logger);
            break;

        case "calculate-bets":
            await RunCalculateBetsAsync(scope.ServiceProvider, logger);
            break;

        case "bot-betting":
            await RunBotBettingAsync(scope.ServiceProvider, logger);
            break;

        case "sync-lineups":
            await RunSyncLineupsAsync(scope.ServiceProvider, logger);
            break;

        default:
            logger.LogError("Unknown command: {Command}", command);
            logger.LogError("Valid commands: sync-competitions, sync-teams, sync-matches, sync-standings, sync-all, calculate-bets, bot-betting, sync-lineups");
            return 1;
    }

    logger.LogInformation("");
    logger.LogInformation("==============================================");
    logger.LogInformation("Operation completed successfully");
    logger.LogInformation("==============================================");
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Operation failed with exception");
    logger.LogInformation("");
    logger.LogInformation("==============================================");
    logger.LogInformation("Operation failed");
    logger.LogInformation("==============================================");
    return 1;
}
finally
{
    scope.Dispose();
    await host.StopAsync();
}

static async Task RunSyncCompetitionsAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting competition sync...");
    logger.LogInformation("");

    var syncService = services.GetRequiredService<IFootballSyncService>();
    var startTime = DateTime.UtcNow;

    await syncService.SyncCompetitionsAsync();

    var duration = DateTime.UtcNow - startTime;
    logger.LogInformation("");
    logger.LogInformation("Competition sync completed in {Duration:N2}s", duration.TotalSeconds);
}

static async Task RunSyncTeamsAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting team sync...");
    logger.LogInformation("");

    var syncService = services.GetRequiredService<IFootballSyncService>();
    var settings = services.GetRequiredService<IOptions<FootballDataSettings>>();
    var startTime = DateTime.UtcNow;

    foreach (var competitionId in settings.Value.SupportedCompetitionIds)
    {
        logger.LogInformation("Syncing teams for competition {Id}...", competitionId);
        await syncService.SyncTeamsForCompetitionAsync(competitionId);
    }

    var duration = DateTime.UtcNow - startTime;
    logger.LogInformation("");
    logger.LogInformation("Team sync completed in {Duration:N2}s", duration.TotalSeconds);
}

static async Task RunSyncMatchesAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting football match sync...");
    logger.LogInformation("");

    var syncService = services.GetRequiredService<IFootballSyncService>();
    var startTime = DateTime.UtcNow;

    await syncService.SyncMatchesAsync();

    var duration = DateTime.UtcNow - startTime;
    logger.LogInformation("");
    logger.LogInformation("Match sync completed in {Duration:N2}s", duration.TotalSeconds);
}

static async Task RunCalculateBetsAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting bet calculation...");
    logger.LogInformation("");

    var betResultsService = services.GetRequiredService<IBetResultsService>();
    var startTime = DateTime.UtcNow;

    var processedCount = await betResultsService.CalculateAllPendingBetResultsAsync();

    var duration = DateTime.UtcNow - startTime;
    logger.LogInformation("");
    logger.LogInformation("Processed {Processed} matches in {Duration:N2}s",
        processedCount, duration.TotalSeconds);
}

static async Task RunSyncStandingsAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting standings sync...");
    logger.LogInformation("");

    var syncService = services.GetRequiredService<IFootballSyncService>();
    var settings = services.GetRequiredService<IOptions<FootballDataSettings>>();

    foreach (var competitionId in settings.Value.SupportedCompetitionIds)
    {
        logger.LogInformation("Syncing standings for competition {Id}...", competitionId);
        var result = await syncService.SyncStandingsForCompetitionAsync(competitionId);

        if (result.NewSeasonDetected)
        {
            logger.LogInformation("New season detected! Syncing teams...");
            await syncService.SyncTeamsForCompetitionAsync(competitionId);
        }
    }

    logger.LogInformation("Standings sync completed");
}

static async Task RunSyncAllAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting full football data sync...");
    logger.LogInformation("This will sync: competitions -> standings (creates seasons) -> teams -> matches");
    logger.LogInformation("");

    var syncService = services.GetRequiredService<IFootballSyncService>();
    var settings = services.GetRequiredService<IOptions<FootballDataSettings>>();
    var startTime = DateTime.UtcNow;

    // Step 1: Sync competitions
    logger.LogInformation("Step 1/4: Syncing competitions...");
    await syncService.SyncCompetitionsAsync();

    // Step 2: Sync standings (creates Season entities + missing teams)
    logger.LogInformation("");
    logger.LogInformation("Step 2/4: Syncing standings (creates seasons)...");
    foreach (var competitionId in settings.Value.SupportedCompetitionIds)
    {
        logger.LogInformation("  Competition {Id}...", competitionId);
        await syncService.SyncStandingsForCompetitionAsync(competitionId);
    }

    // Step 3: Sync teams (ensures all teams for competition are present)
    logger.LogInformation("");
    logger.LogInformation("Step 3/4: Syncing teams...");
    foreach (var competitionId in settings.Value.SupportedCompetitionIds)
    {
        logger.LogInformation("  Competition {Id}...", competitionId);
        await syncService.SyncTeamsForCompetitionAsync(competitionId);
    }

    // Step 4: Sync matches
    logger.LogInformation("");
    logger.LogInformation("Step 4/4: Syncing matches...");
    await syncService.SyncMatchesAsync();

    var duration = DateTime.UtcNow - startTime;
    logger.LogInformation("");
    logger.LogInformation("Full sync completed in {Duration:N2}s", duration.TotalSeconds);
}

static async Task RunBotBettingAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting bot betting...");
    logger.LogInformation("");

    var botService = services.GetRequiredService<IBotBettingService>();
    var startTime = DateTime.UtcNow;

    var betsPlaced = await botService.PlaceBetsForUpcomingMatchesAsync();

    var duration = DateTime.UtcNow - startTime;
    logger.LogInformation("");
    logger.LogInformation("Placed {Count} bets in {Duration:N2}s", betsPlaced, duration.TotalSeconds);
}

static async Task RunSyncLineupsAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting lineup sync for upcoming matches...");

    var lineupSyncService = services.GetRequiredService<ILineupSyncService>();
    var synced = await lineupSyncService.SyncLineupsForUpcomingMatchesAsync(TimeSpan.FromHours(2));

    logger.LogInformation("Synced lineups for {Count} matches", synced);
}
