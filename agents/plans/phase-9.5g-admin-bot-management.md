# Phase 9.5G: Admin Bot Management, Endpoints & Frontend

## Overview
Provide admin CRUD operations for bots, integration health endpoints, external data admin endpoints, bot seed data for new personalities, and frontend components for the admin panel.

> **Prerequisite**: Phases 9.5A-9.5F should be complete

---

## Part 1: New Bot Personalities

### 1.1 Phase 9.5 Bot Seeds

| Bot Name | Strategy | Configuration | Description |
|----------|----------|---------------|-------------|
| Data Scientist | StatsAnalyst | FullAnalysis | Uses all available data sources |
| xG Expert | StatsAnalyst | XgFocused | Heavy xG weighting |
| Market Follower | StatsAnalyst | MarketFollower | Follows betting odds |
| Injury Tracker | StatsAnalyst | InjuryAware | Focuses on squad availability |

**BotSeeder.cs (additions)**:
```csharp
// Phase 9.5 - External data bots
CreateBot("Data Scientist", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.FullAnalysis.ToJson(), "lab_coat"),
CreateBot("xG Expert", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.XgFocused.ToJson(), "chart"),
CreateBot("Market Follower", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.MarketFollower.ToJson(), "money"),
CreateBot("Injury Tracker", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.InjuryAware.ToJson(), "hospital"),
```

---

## Part 2: Bot DTOs, Commands & Queries

### 2.1 DTOs

**File**: `src/ExtraTime.Application/Features/Bots/DTOs/BotDtos.cs`

```csharp
namespace ExtraTime.Application.Features.Bots.DTOs;

public sealed record BotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    string? Configuration,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastBetPlacedAt,
    BotStatsDto? Stats);

public sealed record BotStatsDto(
    int TotalBetsPlaced,
    int LeaguesJoined,
    double AveragePointsPerBet,
    int ExactPredictions,
    int CorrectResults);

public sealed record CreateBotRequest(
    string Name,
    string? AvatarUrl,
    string Strategy,
    Dictionary<string, object>? Configuration);

public sealed record UpdateBotRequest(
    string? Name,
    string? AvatarUrl,
    string? Strategy,
    Dictionary<string, object>? Configuration,
    bool? IsActive);

public sealed record BotConfigurationDto(
    // Form analysis
    double FormWeight,
    double HomeAdvantageWeight,
    double GoalTrendWeight,
    double StreakWeight,

    // External data
    double XgWeight,
    double XgDefensiveWeight,
    double OddsWeight,
    double InjuryWeight,
    double LineupAnalysisWeight,

    // Behavior
    int MatchesAnalyzed,
    bool HighStakesBoost,
    string Style,  // Conservative, Moderate, Bold
    double RandomVariance,

    // Feature flags
    bool UseXgData,
    bool UseOddsData,
    bool UseInjuryData,
    bool UseLineupData);

// Configuration presets for easy bot creation
public sealed record ConfigurationPresetDto(
    string Name,
    string Description,
    BotConfigurationDto Configuration);
```

### 2.2 Commands

**CreateBotCommand.cs**:
```csharp
public sealed record CreateBotCommand(
    string Name,
    string? AvatarUrl,
    BotStrategy Strategy,
    string? Configuration) : IRequest<Result<BotDto>>;

public sealed class CreateBotCommandHandler(
    IApplicationDbContext context,
    ILogger<CreateBotCommandHandler> logger) : IRequestHandler<CreateBotCommand, Result<BotDto>>
{
    public async ValueTask<Result<BotDto>> Handle(
        CreateBotCommand request,
        CancellationToken cancellationToken)
    {
        // Validate name uniqueness
        var existingBot = await context.Bots
            .FirstOrDefaultAsync(b => b.Name == request.Name, cancellationToken);

        if (existingBot != null)
            return Result<BotDto>.Failure($"Bot with name '{request.Name}' already exists");

        // Create bot user account
        var email = $"bot_{request.Name.ToLowerInvariant().Replace(" ", "_")}@extratime.local";
        var password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existingUser != null)
            return Result<BotDto>.Failure("Bot user account already exists");

        var user = User.Register(email, request.Name, password);
        user.MarkAsBot();
        context.Users.Add(user);

        // Create bot
        var bot = new Bot
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = request.Name,
            AvatarUrl = request.AvatarUrl,
            Strategy = request.Strategy,
            Configuration = request.Configuration,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Bots.Add(bot);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created bot {BotName} with strategy {Strategy}",
            request.Name, request.Strategy);

        return Result<BotDto>.Success(MapToDto(bot));
    }

    private static BotDto MapToDto(Bot bot) => new(
        bot.Id,
        bot.Name,
        bot.AvatarUrl,
        bot.Strategy.ToString(),
        bot.Configuration,
        bot.IsActive,
        bot.CreatedAt,
        bot.LastBetPlacedAt,
        null);
}
```

**UpdateBotCommand.cs**:
```csharp
public sealed record UpdateBotCommand(
    Guid BotId,
    string? Name,
    string? AvatarUrl,
    BotStrategy? Strategy,
    string? Configuration,
    bool? IsActive) : IRequest<Result<BotDto>>;

public sealed class UpdateBotCommandHandler(
    IApplicationDbContext context,
    ILogger<UpdateBotCommandHandler> logger) : IRequestHandler<UpdateBotCommand, Result<BotDto>>
{
    public async ValueTask<Result<BotDto>> Handle(
        UpdateBotCommand request,
        CancellationToken cancellationToken)
    {
        var bot = await context.Bots
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == request.BotId, cancellationToken);

        if (bot == null)
            return Result<BotDto>.Failure("Bot not found");

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.Name) && request.Name != bot.Name)
        {
            // Check uniqueness
            var existing = await context.Bots
                .FirstOrDefaultAsync(b => b.Name == request.Name && b.Id != request.BotId, cancellationToken);

            if (existing != null)
                return Result<BotDto>.Failure($"Bot with name '{request.Name}' already exists");

            bot.Name = request.Name;
            bot.User.UpdateProfile(bot.User.Email, request.Name);
        }

        if (request.AvatarUrl != null)
            bot.AvatarUrl = request.AvatarUrl;

        if (request.Strategy.HasValue)
            bot.Strategy = request.Strategy.Value;

        if (request.Configuration != null)
            bot.Configuration = request.Configuration;

        if (request.IsActive.HasValue)
            bot.IsActive = request.IsActive.Value;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated bot {BotId}: {BotName}", bot.Id, bot.Name);

        return Result<BotDto>.Success(MapToDto(bot));
    }

    private static BotDto MapToDto(Bot bot) => new(
        bot.Id,
        bot.Name,
        bot.AvatarUrl,
        bot.Strategy.ToString(),
        bot.Configuration,
        bot.IsActive,
        bot.CreatedAt,
        bot.LastBetPlacedAt,
        null);
}
```

**DeleteBotCommand.cs**:
```csharp
public sealed record DeleteBotCommand(Guid BotId) : IRequest<Result>;

public sealed class DeleteBotCommandHandler(
    IApplicationDbContext context,
    ILogger<DeleteBotCommandHandler> logger) : IRequestHandler<DeleteBotCommand, Result>
{
    public async ValueTask<Result> Handle(
        DeleteBotCommand request,
        CancellationToken cancellationToken)
    {
        var bot = await context.Bots
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == request.BotId, cancellationToken);

        if (bot == null)
            return Result.Failure("Bot not found");

        // Check if bot has placed bets (soft delete consideration)
        var hasBets = await context.Bets
            .AnyAsync(b => b.UserId == bot.UserId, cancellationToken);

        if (hasBets)
        {
            // Soft delete - just deactivate
            bot.IsActive = false;
            logger.LogInformation(
                "Bot {BotName} deactivated (has historical bets)",
                bot.Name);
        }
        else
        {
            // Hard delete - no bets placed
            context.Bots.Remove(bot);
            context.Users.Remove(bot.User);
            logger.LogInformation("Bot {BotName} deleted", bot.Name);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

### 2.3 Queries

**GetBotsQuery.cs**:
```csharp
public sealed record GetBotsQuery(
    bool? IncludeInactive = false,
    BotStrategy? Strategy = null) : IRequest<Result<List<BotDto>>>;

public sealed class GetBotsQueryHandler(
    IApplicationDbContext context) : IRequestHandler<GetBotsQuery, Result<List<BotDto>>>
{
    public async ValueTask<Result<List<BotDto>>> Handle(
        GetBotsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Bots.AsQueryable();

        if (request.IncludeInactive != true)
            query = query.Where(b => b.IsActive);

        if (request.Strategy.HasValue)
            query = query.Where(b => b.Strategy == request.Strategy.Value);

        var bots = await query
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        // Get stats for each bot
        var botDtos = new List<BotDto>();
        foreach (var bot in bots)
        {
            var stats = await GetBotStatsAsync(bot.UserId, cancellationToken);
            botDtos.Add(new BotDto(
                bot.Id,
                bot.Name,
                bot.AvatarUrl,
                bot.Strategy.ToString(),
                bot.Configuration,
                bot.IsActive,
                bot.CreatedAt,
                bot.LastBetPlacedAt,
                stats));
        }

        return Result<List<BotDto>>.Success(botDtos);
    }

    private async Task<BotStatsDto> GetBotStatsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var bets = await context.Bets
            .Include(b => b.Result)
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);

        var leaguesJoined = await context.LeagueBotMembers
            .CountAsync(lbm => lbm.Bot.UserId == userId, cancellationToken);

        var betsWithResults = bets.Where(b => b.Result != null).ToList();

        return new BotStatsDto(
            TotalBetsPlaced: bets.Count,
            LeaguesJoined: leaguesJoined,
            AveragePointsPerBet: betsWithResults.Count > 0
                ? betsWithResults.Average(b => b.Result!.PointsEarned)
                : 0,
            ExactPredictions: betsWithResults.Count(b => b.Result!.IsExactMatch),
            CorrectResults: betsWithResults.Count(b => b.Result!.IsCorrectResult));
    }
}
```

**GetBotConfigurationPresetsQuery.cs**:
```csharp
public sealed record GetBotConfigurationPresetsQuery : IRequest<Result<List<ConfigurationPresetDto>>>;

public sealed class GetBotConfigurationPresetsQueryHandler
    : IRequestHandler<GetBotConfigurationPresetsQuery, Result<List<ConfigurationPresetDto>>>
{
    public ValueTask<Result<List<ConfigurationPresetDto>>> Handle(
        GetBotConfigurationPresetsQuery request,
        CancellationToken cancellationToken)
    {
        var presets = new List<ConfigurationPresetDto>
        {
            new("Balanced", "All-round analysis using all available data",
                MapConfig(StatsAnalystConfig.Balanced)),
            new("Form Focused", "Heavily weights recent match results",
                MapConfig(StatsAnalystConfig.FormFocused)),
            new("Home Advantage", "Believes home teams always win",
                MapConfig(StatsAnalystConfig.HomeAdvantage)),
            new("Goal Focused", "Predicts high-scoring matches",
                MapConfig(StatsAnalystConfig.GoalFocused)),
            new("Conservative", "Low-risk, low-score predictions",
                MapConfig(StatsAnalystConfig.Conservative)),
            new("Chaotic", "Unpredictable wild predictions",
                MapConfig(StatsAnalystConfig.Chaotic)),
            new("Full Analysis", "Uses all external data sources",
                MapConfig(StatsAnalystConfig.FullAnalysis)),
            new("xG Expert", "Heavy expected goals weighting",
                MapConfig(StatsAnalystConfig.XgFocused)),
            new("Market Follower", "Follows betting odds consensus",
                MapConfig(StatsAnalystConfig.MarketFollower)),
            new("Injury Aware", "Focuses on squad availability",
                MapConfig(StatsAnalystConfig.InjuryAware)),
        };

        return ValueTask.FromResult(Result<List<ConfigurationPresetDto>>.Success(presets));
    }

    private static BotConfigurationDto MapConfig(StatsAnalystConfig config) => new(
        FormWeight: config.FormWeight,
        HomeAdvantageWeight: config.HomeAdvantageWeight,
        GoalTrendWeight: config.GoalTrendWeight,
        StreakWeight: config.StreakWeight,
        XgWeight: config.XgWeight,
        XgDefensiveWeight: config.XgDefensiveWeight,
        OddsWeight: config.OddsWeight,
        InjuryWeight: config.InjuryWeight,
        LineupAnalysisWeight: config.LineupAnalysisWeight,
        MatchesAnalyzed: config.MatchesAnalyzed,
        HighStakesBoost: config.HighStakesBoost,
        Style: config.Style.ToString(),
        RandomVariance: config.RandomVariance,
        UseXgData: config.UseXgData,
        UseOddsData: config.UseOddsData,
        UseInjuryData: config.UseInjuryData,
        UseLineupData: config.UseLineupData);
}
```

---

## Part 3: Admin Endpoints

### 3.1 AdminBotsEndpoints

**File**: `src/ExtraTime.API/Endpoints/AdminBotsEndpoints.cs`

```csharp
public static class AdminBotsEndpoints
{
    public static RouteGroupBuilder MapAdminBotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/bots")
            .WithTags("Admin - Bots")
            .RequireAuthorization("AdminOnly");

        // CRUD
        group.MapGet("/", GetBots).WithName("AdminGetBots");
        group.MapGet("/{id:guid}", GetBot).WithName("AdminGetBot");
        group.MapPost("/", CreateBot).WithName("AdminCreateBot");
        group.MapPut("/{id:guid}", UpdateBot).WithName("AdminUpdateBot");
        group.MapDelete("/{id:guid}", DeleteBot).WithName("AdminDeleteBot");

        // Configuration
        group.MapGet("/presets", GetPresets).WithName("GetBotPresets");
        group.MapPost("/validate-config", ValidateConfig).WithName("ValidateBotConfig");

        // Actions
        group.MapPost("/{id:guid}/activate", ActivateBot).WithName("ActivateBot");
        group.MapPost("/{id:guid}/deactivate", DeactivateBot).WithName("DeactivateBot");
        group.MapPost("/trigger-betting", TriggerBotBetting).WithName("TriggerBotBetting");

        return group;
    }

    private static async Task<IResult> GetBots(
        [AsParameters] GetBotsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetBot(
        Guid id,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var bot = await context.Bots
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        return bot != null ? Results.Ok(bot) : Results.NotFound();
    }

    private static async Task<IResult> CreateBot(
        CreateBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BotStrategy>(request.Strategy, true, out var strategy))
            return Results.BadRequest(new { error = "Invalid strategy" });

        var configJson = request.Configuration != null
            ? JsonSerializer.Serialize(request.Configuration)
            : null;

        var command = new CreateBotCommand(
            request.Name,
            request.AvatarUrl,
            strategy,
            configJson);

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/admin/bots/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> UpdateBot(
        Guid id,
        UpdateBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        BotStrategy? strategy = null;
        if (!string.IsNullOrEmpty(request.Strategy))
        {
            if (!Enum.TryParse<BotStrategy>(request.Strategy, true, out var parsed))
                return Results.BadRequest(new { error = "Invalid strategy" });
            strategy = parsed;
        }

        var configJson = request.Configuration != null
            ? JsonSerializer.Serialize(request.Configuration)
            : null;

        var command = new UpdateBotCommand(
            id,
            request.Name,
            request.AvatarUrl,
            strategy,
            configJson,
            request.IsActive);

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> DeleteBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteBotCommand(id), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetPresets(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBotConfigurationPresetsQuery(), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static IResult ValidateConfig(BotConfigurationDto config)
    {
        var errors = new List<string>();

        // Validate weights sum to approximately 1.0
        var totalWeight = config.FormWeight + config.HomeAdvantageWeight +
                         config.GoalTrendWeight + config.StreakWeight +
                         config.XgWeight + config.XgDefensiveWeight +
                         config.OddsWeight + config.InjuryWeight +
                         config.LineupAnalysisWeight;

        if (totalWeight < 0.8 || totalWeight > 1.2)
            errors.Add($"Weights should sum to approximately 1.0 (current: {totalWeight:F2})");

        // Validate ranges
        if (config.RandomVariance < 0 || config.RandomVariance > 0.5)
            errors.Add("RandomVariance must be between 0 and 0.5");

        if (config.MatchesAnalyzed < 3 || config.MatchesAnalyzed > 20)
            errors.Add("MatchesAnalyzed must be between 3 and 20");

        if (!Enum.TryParse<PredictionStyle>(config.Style, true, out _))
            errors.Add("Invalid Style value");

        return errors.Count > 0
            ? Results.BadRequest(new { valid = false, errors })
            : Results.Ok(new { valid = true, errors = Array.Empty<string>() });
    }

    private static async Task<IResult> ActivateBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBotCommand(id, null, null, null, null, true);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> DeactivateBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBotCommand(id, null, null, null, null, false);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> TriggerBotBetting(
        IBotBettingService botService,
        CancellationToken cancellationToken)
    {
        var betsPlaced = await botService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);
        return Results.Ok(new { betsPlaced });
    }
}
```

### 3.2 AdminIntegrationEndpoints

**File**: `src/ExtraTime.API/Endpoints/AdminIntegrationEndpoints.cs`

```csharp
public static class AdminIntegrationEndpoints
{
    public static RouteGroupBuilder MapAdminIntegrationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/integrations")
            .WithTags("Admin - Integrations")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAllStatuses).WithName("GetIntegrationStatuses");
        group.MapGet("/{type}", GetStatus).WithName("GetIntegrationStatus");
        group.MapPost("/{type}/disable", DisableIntegration).WithName("DisableIntegration");
        group.MapPost("/{type}/enable", EnableIntegration).WithName("EnableIntegration");
        group.MapPost("/{type}/sync", TriggerSync).WithName("TriggerIntegrationSync");
        group.MapGet("/availability", GetDataAvailability).WithName("GetDataAvailability");

        return group;
    }

    private static async Task<IResult> GetAllStatuses(
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        var statuses = await healthService.GetAllStatusesAsync(cancellationToken);
        return Results.Ok(statuses);
    }

    private static async Task<IResult> GetStatus(
        string type,
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
            return Results.BadRequest(new { error = "Invalid integration type" });

        var status = await healthService.GetStatusAsync(integrationType, cancellationToken);
        return Results.Ok(status);
    }

    private static async Task<IResult> DisableIntegration(
        string type,
        DisableIntegrationRequest request,
        IIntegrationHealthService healthService,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
            return Results.BadRequest(new { error = "Invalid integration type" });

        await healthService.DisableIntegrationAsync(
            integrationType,
            request.Reason,
            currentUser.UserId.ToString(),
            cancellationToken);

        return Results.Ok(new { message = $"{type} disabled" });
    }

    private static async Task<IResult> EnableIntegration(
        string type,
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
            return Results.BadRequest(new { error = "Invalid integration type" });

        await healthService.EnableIntegrationAsync(integrationType, cancellationToken);
        return Results.Ok(new { message = $"{type} enabled" });
    }

    private static async Task<IResult> TriggerSync(
        string type,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
            return Results.BadRequest(new { error = "Invalid integration type" });

        try
        {
            switch (integrationType)
            {
                case IntegrationType.Understat:
                    var understat = serviceProvider.GetRequiredService<IUnderstatService>();
                    await understat.SyncAllLeaguesAsync(cancellationToken);
                    break;
                case IntegrationType.FootballDataUk:
                    var odds = serviceProvider.GetRequiredService<IOddsDataService>();
                    await odds.ImportAllLeaguesAsync(cancellationToken);
                    break;
                case IntegrationType.ApiFootball:
                    var injuries = serviceProvider.GetRequiredService<IInjuryService>();
                    await injuries.SyncInjuriesForUpcomingMatchesAsync(3, cancellationToken);
                    break;
                case IntegrationType.FootballDataOrg:
                    var football = serviceProvider.GetRequiredService<IFootballSyncService>();
                    await football.SyncMatchesAsync(cancellationToken);
                    break;
                case IntegrationType.ClubElo:
                    var elo = serviceProvider.GetRequiredService<IEloRatingService>();
                    await elo.SyncEloRatingsAsync(cancellationToken);
                    break;
            }

            return Results.Ok(new { message = $"{type} sync triggered" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetDataAvailability(
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        var availability = await healthService.GetDataAvailabilityAsync(cancellationToken);
        return Results.Ok(availability);
    }
}

public sealed record DisableIntegrationRequest(string Reason);
```

### 3.3 AdminExternalDataEndpoints

**File**: `src/ExtraTime.API/Endpoints/AdminExternalDataEndpoints.cs`

```csharp
public static class AdminExternalDataEndpoints
{
    public static RouteGroupBuilder MapAdminExternalDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/external-data")
            .WithTags("Admin - External Data")
            .RequireAuthorization("AdminOnly");

        // Understat
        group.MapPost("/understat/sync", SyncUnderstat)
            .WithName("SyncUnderstat");

        group.MapGet("/understat/stats/{teamId:guid}", GetTeamXgStats)
            .WithName("GetTeamXgStats");

        // Odds
        group.MapPost("/odds/sync", SyncOdds)
            .WithName("SyncOdds");

        group.MapGet("/odds/{matchId:guid}", GetMatchOdds)
            .WithName("GetMatchOdds");

        // Injuries
        group.MapPost("/injuries/sync", SyncInjuries)
            .WithName("SyncInjuries");

        group.MapGet("/injuries/{teamId:guid}", GetTeamInjuries)
            .WithName("GetTeamInjuries");

        return group;
    }

    private static async Task<IResult> SyncUnderstat(
        IUnderstatService service,
        CancellationToken cancellationToken)
    {
        await service.SyncAllLeaguesAsync(cancellationToken);
        return Results.Ok(new { message = "Understat sync started" });
    }

    private static async Task<IResult> SyncOdds(
        IOddsDataService service,
        CancellationToken cancellationToken)
    {
        await service.ImportAllLeaguesAsync(cancellationToken);
        return Results.Ok(new { message = "Odds import started" });
    }

    private static async Task<IResult> SyncInjuries(
        IInjuryService service,
        CancellationToken cancellationToken)
    {
        await service.SyncInjuriesForUpcomingMatchesAsync(3, cancellationToken);
        return Results.Ok(new { message = "Injuries sync started" });
    }

    private static async Task<IResult> GetTeamXgStats(
        Guid teamId,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var stats = await context.TeamXgStats
            .Where(x => x.TeamId == teamId)
            .OrderByDescending(x => x.Season)
            .FirstOrDefaultAsync(cancellationToken);

        return stats != null ? Results.Ok(stats) : Results.NotFound();
    }

    private static async Task<IResult> GetMatchOdds(
        Guid matchId,
        IOddsDataService service,
        CancellationToken cancellationToken)
    {
        var odds = await service.GetOddsForMatchAsync(matchId, cancellationToken);
        return odds != null ? Results.Ok(odds) : Results.NotFound();
    }

    private static async Task<IResult> GetTeamInjuries(
        Guid teamId,
        IInjuryService service,
        CancellationToken cancellationToken)
    {
        var injuries = await service.GetTeamInjuriesAsync(teamId, cancellationToken);
        return injuries != null ? Results.Ok(injuries) : Results.NotFound();
    }
}
```

---

## Part 4: Frontend Components

### 4.1 Bot Management Page

**File**: `app/(admin)/admin/bots/page.tsx`

```typescript
'use client';

import { useState } from 'react';
import { useBots, useDeleteBot } from '@/hooks/use-admin-bots';
import { BotCard } from '@/components/admin/bot-card';
import { CreateBotModal } from '@/components/admin/create-bot-modal';
import { Button } from '@/components/ui/button';
import { Plus, Bot as BotIcon } from 'lucide-react';

export default function AdminBotsPage() {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const { data: bots, isLoading } = useBots({ includeInactive: true });
  const deleteBot = useDeleteBot();

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Bot Management</h1>
          <p className="text-muted-foreground">
            Create and configure AI betting bots
          </p>
        </div>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Create Bot
        </Button>
      </div>

      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="h-48 animate-pulse rounded-lg bg-muted" />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {bots?.map((bot) => (
            <BotCard
              key={bot.id}
              bot={bot}
              onDelete={() => deleteBot.mutate(bot.id)}
            />
          ))}
        </div>
      )}

      <CreateBotModal
        open={showCreateModal}
        onOpenChange={setShowCreateModal}
      />
    </div>
  );
}
```

### 4.2 BotCard Component

**File**: `components/admin/bot-card.tsx`

```typescript
'use client';

import { Bot } from '@/types/bot';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { MoreVertical, Edit, Trash2, Power, PowerOff } from 'lucide-react';
import { useUpdateBot } from '@/hooks/use-admin-bots';
import { useState } from 'react';
import { EditBotModal } from './edit-bot-modal';

interface BotCardProps {
  bot: Bot;
  onDelete: () => void;
}

const strategyEmojis: Record<string, string> = {
  Random: 'dice',
  HomeFavorer: 'home',
  UnderdogSupporter: 'dog',
  DrawPredictor: 'handshake',
  HighScorer: 'football',
  StatsAnalyst: 'brain',
};

export function BotCard({ bot, onDelete }: BotCardProps) {
  const [showEditModal, setShowEditModal] = useState(false);
  const updateBot = useUpdateBot();

  const toggleActive = () => {
    updateBot.mutate({ id: bot.id, isActive: !bot.isActive });
  };

  return (
    <>
      <Card className={bot.isActive ? '' : 'opacity-60'}>
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <div className="flex items-center gap-2">
            <CardTitle className="text-lg">{bot.name}</CardTitle>
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => setShowEditModal(true)}>
                <Edit className="mr-2 h-4 w-4" />
                Edit
              </DropdownMenuItem>
              <DropdownMenuItem onClick={toggleActive}>
                {bot.isActive ? (
                  <>
                    <PowerOff className="mr-2 h-4 w-4" />
                    Deactivate
                  </>
                ) : (
                  <>
                    <Power className="mr-2 h-4 w-4" />
                    Activate
                  </>
                )}
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={onDelete}
                className="text-destructive"
              >
                <Trash2 className="mr-2 h-4 w-4" />
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Badge variant={bot.isActive ? 'default' : 'secondary'}>
                {bot.isActive ? 'Active' : 'Inactive'}
              </Badge>
              <Badge variant="outline">{bot.strategy}</Badge>
            </div>

            {bot.stats && (
              <div className="grid grid-cols-2 gap-2 text-sm">
                <div>
                  <span className="text-muted-foreground">Bets placed:</span>
                  <span className="ml-1 font-medium">{bot.stats.totalBetsPlaced}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Leagues:</span>
                  <span className="ml-1 font-medium">{bot.stats.leaguesJoined}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Avg pts:</span>
                  <span className="ml-1 font-medium">
                    {bot.stats.averagePointsPerBet.toFixed(2)}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground">Exact:</span>
                  <span className="ml-1 font-medium">{bot.stats.exactPredictions}</span>
                </div>
              </div>
            )}

            {bot.lastBetPlacedAt && (
              <p className="text-xs text-muted-foreground">
                Last bet: {new Date(bot.lastBetPlacedAt).toLocaleDateString()}
              </p>
            )}
          </div>
        </CardContent>
      </Card>

      <EditBotModal
        bot={bot}
        open={showEditModal}
        onOpenChange={setShowEditModal}
      />
    </>
  );
}
```

### 4.3 Integration Health Dashboard

**File**: `app/(admin)/admin/integrations/page.tsx`

```typescript
'use client';

import { useIntegrationStatuses, useDataAvailability } from '@/hooks/use-admin-integrations';
import { IntegrationCard } from '@/components/admin/integration-card';
import { DataAvailabilityCard } from '@/components/admin/data-availability-card';

export default function AdminIntegrationsPage() {
  const { data: statuses, isLoading } = useIntegrationStatuses();
  const { data: availability } = useDataAvailability();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Integration Health</h1>
        <p className="text-muted-foreground">
          Monitor external data sources and their availability
        </p>
      </div>

      {/* Data Availability Summary */}
      {availability && <DataAvailabilityCard availability={availability} />}

      {/* Integration Cards */}
      <div className="grid gap-4 md:grid-cols-2">
        {isLoading ? (
          [...Array(4)].map((_, i) => (
            <div key={i} className="h-48 animate-pulse rounded-lg bg-muted" />
          ))
        ) : (
          statuses?.map((status) => (
            <IntegrationCard key={status.integrationName} status={status} />
          ))
        )}
      </div>
    </div>
  );
}
```

### 4.4 Admin Hooks

**File**: `hooks/use-admin-bots.ts`

```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import { Bot, CreateBotRequest, UpdateBotRequest, ConfigurationPreset } from '@/types/bot';

export function useBots(options?: { includeInactive?: boolean; strategy?: string }) {
  return useQuery({
    queryKey: ['admin', 'bots', options],
    queryFn: () => {
      const params = new URLSearchParams();
      if (options?.includeInactive) params.set('includeInactive', 'true');
      if (options?.strategy) params.set('strategy', options.strategy);
      return apiClient.get<Bot[]>(`/admin/bots?${params}`);
    },
  });
}

export function useBotPresets() {
  return useQuery({
    queryKey: ['admin', 'bots', 'presets'],
    queryFn: () => apiClient.get<ConfigurationPreset[]>('/admin/bots/presets'),
  });
}

export function useCreateBot() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBotRequest) =>
      apiClient.post<Bot>('/admin/bots', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'bots'] });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}

export function useUpdateBot() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, ...data }: UpdateBotRequest & { id: string }) =>
      apiClient.put<Bot>(`/admin/bots/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'bots'] });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}

export function useDeleteBot() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/admin/bots/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'bots'] });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}
```

**File**: `hooks/use-admin-integrations.ts`

```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import { IntegrationStatus, DataAvailability } from '@/types/integration';

export function useIntegrationStatuses() {
  return useQuery({
    queryKey: ['admin', 'integrations'],
    queryFn: () => apiClient.get<IntegrationStatus[]>('/admin/integrations'),
    refetchInterval: 30000, // Refresh every 30 seconds
  });
}

export function useDataAvailability() {
  return useQuery({
    queryKey: ['admin', 'integrations', 'availability'],
    queryFn: () => apiClient.get<DataAvailability>('/admin/integrations/availability'),
    refetchInterval: 60000,
  });
}

export function useTriggerSync() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (type: string) =>
      apiClient.post(`/admin/integrations/${type}/sync`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'integrations'] });
    },
  });
}

export function useToggleIntegration() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ type, enable }: { type: string; enable: boolean }) =>
      enable
        ? apiClient.post(`/admin/integrations/${type}/enable`)
        : apiClient.post(`/admin/integrations/${type}/disable`, { reason: 'Manual disable' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'integrations'] });
    },
  });
}
```

---

## Implementation Checklist

### Phase 9.5G: Admin Bot Management
- [ ] Create `CreateBotCommand` + Handler
- [ ] Create `UpdateBotCommand` + Handler
- [ ] Create `DeleteBotCommand` + Handler
- [ ] Create `GetBotsQuery` + Handler (with stats)
- [ ] Create `GetBotConfigurationPresetsQuery` + Handler
- [ ] Create `AdminBotsEndpoints`
- [ ] Create `AdminIntegrationEndpoints` (with ClubElo sync trigger)
- [ ] Create `AdminExternalDataEndpoints`
- [ ] Register endpoints in Program.cs
- [ ] Test bot CRUD operations

### Phase 9.5H: Admin Frontend
- [ ] Create `app/(admin)/admin/bots/page.tsx`
- [ ] Create `BotCard` component
- [ ] Create `CreateBotModal` component
- [ ] Create `EditBotModal` component
- [ ] Create `app/(admin)/admin/integrations/page.tsx`
- [ ] Create `IntegrationCard` component
- [ ] Create `DataAvailabilityCard` component
- [ ] Create `use-admin-bots.ts` hooks
- [ ] Create `use-admin-integrations.ts` hooks
- [ ] Add admin navigation links

### Phase 9.5I: New Bots
- [ ] Update `BotSeeder` with 4 new bots
- [ ] Test new bot predictions
- [ ] Compare bot performance

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Application/Features/Bots/DTOs/BotDtos.cs` |
| **Create** | `Application/Features/Bots/Commands/CreateBotCommand.cs` |
| **Create** | `Application/Features/Bots/Commands/UpdateBotCommand.cs` |
| **Create** | `Application/Features/Bots/Commands/DeleteBotCommand.cs` |
| **Create** | `Application/Features/Bots/Queries/GetBotsQuery.cs` |
| **Create** | `Application/Features/Bots/Queries/GetBotConfigurationPresetsQuery.cs` |
| **Create** | `API/Endpoints/AdminBotsEndpoints.cs` |
| **Create** | `API/Endpoints/AdminIntegrationEndpoints.cs` |
| **Create** | `API/Endpoints/AdminExternalDataEndpoints.cs` |
| **Create** | `client/app/(admin)/admin/bots/page.tsx` |
| **Create** | `client/components/admin/bot-card.tsx` |
| **Create** | `client/components/admin/create-bot-modal.tsx` |
| **Create** | `client/app/(admin)/admin/integrations/page.tsx` |
| **Create** | `client/hooks/use-admin-bots.ts` |
| **Create** | `client/hooks/use-admin-integrations.ts` |
| **Modify** | `Infrastructure/Data/BotSeeder.cs` |
| **Modify** | `API/Program.cs` (register endpoints) |
