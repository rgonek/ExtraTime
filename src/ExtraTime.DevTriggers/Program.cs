using ExtraTime.Application;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
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
var host = builder.Build();

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
        case "sync-matches":
            await RunSyncMatchesAsync(scope.ServiceProvider, logger);
            break;

        case "calculate-bets":
            await RunCalculateBetsAsync(scope.ServiceProvider, logger);
            break;

        case "bot-betting":
            await RunBotBettingAsync(scope.ServiceProvider, logger);
            break;

        default:
            logger.LogError("Unknown command: {Command}", command);
            logger.LogError("Valid commands: sync-matches, calculate-bets, bot-betting");
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

    var context = services.GetRequiredService<IApplicationDbContext>();
    var mediator = services.GetRequiredService<IMediator>();
    var startTime = DateTime.UtcNow;

    // Find all finished matches with uncalculated bets
    var uncalculatedMatches = await context.Bets
        .Include(b => b.Match)
        .Where(b => b.Match.Status == MatchStatus.Finished
                 && b.Match.HomeScore.HasValue
                 && b.Match.AwayScore.HasValue
                 && b.Result == null)
        .Select(b => new { b.Match.Id, b.Match.CompetitionId })
        .Distinct()
        .ToListAsync();

    logger.LogInformation("Found {Count} matches with uncalculated bets", uncalculatedMatches.Count);
    logger.LogInformation("");

    if (uncalculatedMatches.Count == 0)
    {
        logger.LogInformation("No bets to calculate");
        return;
    }

    var processedCount = 0;
    foreach (var match in uncalculatedMatches)
    {
        logger.LogInformation("Processing match {MatchId}...", match.Id);
        var command = new CalculateBetResultsCommand(match.Id, match.CompetitionId);
        var result = await mediator.Send(command);

        if (result.IsSuccess)
        {
            processedCount++;
            logger.LogInformation("✓ Match {MatchId} processed successfully", match.Id);
        }
        else
        {
            logger.LogWarning("✗ Match {MatchId} processing failed: {Error}", match.Id, result.Error);
        }
    }

    var duration = DateTime.UtcNow - startTime;
    logger.LogInformation("");
    logger.LogInformation("Processed {Processed}/{Total} matches in {Duration:N2}s",
        processedCount, uncalculatedMatches.Count, duration.TotalSeconds);
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
