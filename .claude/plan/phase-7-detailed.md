# Phase 7: Bot System - Detailed Implementation Plan ✅ COMPLETE

## Overview
Add AI bots to make leagues feel active and competitive. Bots use predefined betting strategies to place bets on matches, appearing in leaderboards alongside real users.

---

## Part 1: Domain Layer - Bot Entity & Strategy

### 1.1 New Entities

**Bot Entity Structure:**
```
src/ExtraTime.Domain/Entities/Bot.cs
```

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Primary key |
| UserId | Guid | FK to User (each bot has a user account) |
| User | User | Navigation property |
| Name | string | Display name (max 50) |
| AvatarUrl | string? | Bot avatar image URL |
| Strategy | BotStrategy | Betting strategy enum |
| Configuration | string? | JSON config for strategy parameters |
| IsActive | bool | Whether bot is actively placing bets |
| CreatedAt | DateTime | Creation timestamp |
| LastBetPlacedAt | DateTime? | Last activity timestamp |

**LeagueBotMember Entity (Join Table):**
```
src/ExtraTime.Domain/Entities/LeagueBotMember.cs
```

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Primary key |
| LeagueId | Guid | FK to League |
| League | League | Navigation property |
| BotId | Guid | FK to Bot |
| Bot | Bot | Navigation property |
| AddedAt | DateTime | When bot was added to league |

### 1.2 New Enums

**BotStrategy Enum:**
```
src/ExtraTime.Domain/Enums/BotStrategy.cs
```

```csharp
public enum BotStrategy
{
    Random = 0,              // Random scores within reasonable range
    HomeFavorer = 1,         // Predicts home team advantage
    UnderdogSupporter = 2,   // Favors away team upsets
    DrawPredictor = 3,       // Predicts draws more often
    HighScorer = 4           // Predicts high-scoring matches
}
```

### 1.3 Domain Changes

**Update User Entity:**
- Add `IsBot` property (bool) to distinguish bot accounts
- Add navigation: `Bot? Bot` (one-to-one relationship)

**Update League Entity:**
- Add `BotsEnabled` property (bool, default false)
- Add navigation: `ICollection<LeagueBotMember> BotMembers`
- Add method: `void AddBot(Guid botId)`
- Add method: `void RemoveBot(Guid botId)`

---

## Part 2: Infrastructure Layer - Database Configuration

### 2.1 Entity Configurations

**BotConfiguration.cs:**
```csharp
public sealed class BotConfiguration : IEntityTypeConfiguration<Bot>
{
    public void Configure(EntityTypeBuilder<Bot> builder)
    {
        builder.ToTable("Bots");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(b => b.Strategy)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(b => b.Configuration)
            .HasMaxLength(2000);

        builder.HasOne(b => b.User)
            .WithOne(u => u.Bot)
            .HasForeignKey<Bot>(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.UserId)
            .IsUnique();
    }
}
```

**LeagueBotMemberConfiguration.cs:**
```csharp
public sealed class LeagueBotMemberConfiguration : IEntityTypeConfiguration<LeagueBotMember>
{
    public void Configure(EntityTypeBuilder<LeagueBotMember> builder)
    {
        builder.ToTable("LeagueBotMembers");

        builder.HasKey(lbm => lbm.Id);

        builder.HasOne(lbm => lbm.League)
            .WithMany(l => l.BotMembers)
            .HasForeignKey(lbm => lbm.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lbm => lbm.Bot)
            .WithMany()
            .HasForeignKey(lbm => lbm.BotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(lbm => new { lbm.LeagueId, lbm.BotId })
            .IsUnique();
    }
}
```

### 2.2 Update ApplicationDbContext

Add DbSets:
```csharp
public DbSet<Bot> Bots => Set<Bot>();
public DbSet<LeagueBotMember> LeagueBotMembers => Set<LeagueBotMember>();
```

### 2.3 Migration

Create migration: `AddBotSystem`
- Creates `Bots` table
- Creates `LeagueBotMembers` table
- Adds `IsBot` column to `Users` table
- Adds `BotsEnabled` column to `Leagues` table

---

## Part 3: Application Layer - Bot Strategies

### 3.1 Strategy Interface & Implementations

**Directory Structure:**
```
src/ExtraTime.Application/Features/Bots/
├── Commands/
│   ├── CreateBot/
│   │   ├── CreateBotCommand.cs
│   │   ├── CreateBotCommandHandler.cs
│   │   └── CreateBotCommandValidator.cs
│   ├── AddBotToLeague/
│   │   ├── AddBotToLeagueCommand.cs
│   │   ├── AddBotToLeagueCommandHandler.cs
│   │   └── AddBotToLeagueCommandValidator.cs
│   ├── RemoveBotFromLeague/
│   │   ├── RemoveBotFromLeagueCommand.cs
│   │   └── RemoveBotFromLeagueCommandHandler.cs
│   └── PlaceBotBets/
│       ├── PlaceBotBetsCommand.cs
│       └── PlaceBotBetsCommandHandler.cs
├── Queries/
│   ├── GetBots/
│   │   ├── GetBotsQuery.cs
│   │   └── GetBotsQueryHandler.cs
│   └── GetLeagueBots/
│       ├── GetLeagueBotsQuery.cs
│       └── GetLeagueBotsQueryHandler.cs
├── DTOs/
│   └── BotDtos.cs
├── Strategies/
│   ├── IBotBettingStrategy.cs
│   ├── RandomStrategy.cs
│   ├── HomeFavorerStrategy.cs
│   ├── UnderdogSupporterStrategy.cs
│   ├── DrawPredictorStrategy.cs
│   └── HighScorerStrategy.cs
└── Services/
    ├── IBotBettingService.cs
    └── BotBettingService.cs
```

### 3.2 Strategy Interface

**IBotBettingStrategy.cs:**
```csharp
public interface IBotBettingStrategy
{
    BotStrategy StrategyType { get; }
    (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration);
}
```

### 3.3 Strategy Implementations

**RandomStrategy.cs:**
```csharp
public sealed class RandomStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.Random;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        // Random scores between 0-4 with weighted probability
        // Most common: 1-1, 2-1, 1-0, 2-0
        return (_random.Next(0, 5), _random.Next(0, 4));
    }
}
```

**HomeFavorerStrategy.cs:**
```csharp
public sealed class HomeFavorerStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.HomeFavorer;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        // Home team scores more often
        int homeScore = _random.Next(1, 4);  // 1-3 goals
        int awayScore = _random.Next(0, homeScore); // Less than home
        return (homeScore, awayScore);
    }
}
```

**UnderdogSupporterStrategy.cs:**
```csharp
public sealed class UnderdogSupporterStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.UnderdogSupporter;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        // Away team wins or draws more often
        int awayScore = _random.Next(1, 4);
        int homeScore = _random.Next(0, awayScore + 1);
        return (homeScore, awayScore);
    }
}
```

**DrawPredictorStrategy.cs:**
```csharp
public sealed class DrawPredictorStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.DrawPredictor;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        // 70% draws, 30% slight wins
        if (_random.NextDouble() < 0.7)
        {
            int score = _random.Next(0, 3); // 0-0, 1-1, 2-2
            return (score, score);
        }
        int winner = _random.Next(1, 3);
        int loser = winner - 1;
        return _random.NextDouble() < 0.5 ? (winner, loser) : (loser, winner);
    }
}
```

**HighScorerStrategy.cs:**
```csharp
public sealed class HighScorerStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.HighScorer;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        // High scoring matches: 3-2, 4-1, 2-3, etc.
        int homeScore = _random.Next(2, 5);  // 2-4 goals
        int awayScore = _random.Next(1, 4);  // 1-3 goals
        return (homeScore, awayScore);
    }
}
```

### 3.4 Strategy Factory

**BotStrategyFactory.cs:**
```csharp
public sealed class BotStrategyFactory
{
    private readonly Dictionary<BotStrategy, IBotBettingStrategy> _strategies;

    public BotStrategyFactory()
    {
        _strategies = new Dictionary<BotStrategy, IBotBettingStrategy>
        {
            { BotStrategy.Random, new RandomStrategy() },
            { BotStrategy.HomeFavorer, new HomeFavorerStrategy() },
            { BotStrategy.UnderdogSupporter, new UnderdogSupporterStrategy() },
            { BotStrategy.DrawPredictor, new DrawPredictorStrategy() },
            { BotStrategy.HighScorer, new HighScorerStrategy() }
        };
    }

    public IBotBettingStrategy GetStrategy(BotStrategy strategy)
    {
        return _strategies.TryGetValue(strategy, out var impl)
            ? impl
            : _strategies[BotStrategy.Random];
    }
}
```

### 3.5 Bot Betting Service

**IBotBettingService.cs:**
```csharp
public interface IBotBettingService
{
    Task<int> PlaceBetsForUpcomingMatchesAsync(CancellationToken cancellationToken = default);
    Task<int> PlaceBetsForLeagueAsync(Guid leagueId, CancellationToken cancellationToken = default);
}
```

**BotBettingService.cs:**
```csharp
public sealed class BotBettingService(
    IApplicationDbContext context,
    BotStrategyFactory strategyFactory,
    TimeProvider timeProvider,
    ILogger<BotBettingService> logger) : IBotBettingService
{
    public async Task<int> PlaceBetsForUpcomingMatchesAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoffTime = now.AddHours(24); // Look ahead 24 hours

        // Get all leagues with bots enabled
        var leagues = await context.Leagues
            .Include(l => l.BotMembers)
                .ThenInclude(bm => bm.Bot)
            .Where(l => l.BotsEnabled && l.BotMembers.Any())
            .ToListAsync(cancellationToken);

        int totalBetsPlaced = 0;

        foreach (var league in leagues)
        {
            var betsPlaced = await PlaceBetsForLeagueInternalAsync(league, now, cutoffTime, cancellationToken);
            totalBetsPlaced += betsPlaced;
        }

        return totalBetsPlaced;
    }

    private async Task<int> PlaceBetsForLeagueInternalAsync(
        League league,
        DateTime now,
        DateTime cutoffTime,
        CancellationToken cancellationToken)
    {
        // Get upcoming matches for this league's allowed competitions
        var matches = await context.Matches
            .Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Timed)
            .Where(m => m.MatchDateUtc > now && m.MatchDateUtc <= cutoffTime)
            .Where(m => league.CanAcceptBet(m.CompetitionId))
            .ToListAsync(cancellationToken);

        int betsPlaced = 0;

        foreach (var botMember in league.BotMembers)
        {
            var bot = botMember.Bot;
            if (!bot.IsActive) continue;

            var strategy = strategyFactory.GetStrategy(bot.Strategy);

            foreach (var match in matches)
            {
                // Check if bot already has a bet for this match
                var existingBet = await context.Bets
                    .FirstOrDefaultAsync(b =>
                        b.LeagueId == league.Id &&
                        b.UserId == bot.UserId &&
                        b.MatchId == match.Id,
                        cancellationToken);

                if (existingBet != null) continue;

                // Check if match is open for betting
                if (!match.IsOpenForBetting(league.BettingDeadlineMinutes, now)) continue;

                // Generate prediction
                var (homeScore, awayScore) = strategy.GeneratePrediction(match, bot.Configuration);

                // Place bet
                var bet = Bet.Place(
                    league.Id,
                    bot.UserId,
                    match.Id,
                    homeScore,
                    awayScore,
                    match,
                    league.BettingDeadlineMinutes);

                context.Bets.Add(bet);
                betsPlaced++;

                logger.LogDebug(
                    "Bot {BotName} placed bet {HomeScore}-{AwayScore} for match {MatchId} in league {LeagueId}",
                    bot.Name, homeScore, awayScore, match.Id, league.Id);
            }

            // Update bot's last activity
            bot.LastBetPlacedAt = now;
        }

        if (betsPlaced > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return betsPlaced;
    }
}
```

---

## Part 4: Application Layer - Commands & Queries

### 4.1 DTOs

**BotDtos.cs:**
```csharp
public sealed record BotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastBetPlacedAt);

public sealed record BotSummaryDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy);

public sealed record LeagueBotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    DateTime AddedAt);

public sealed record CreateBotRequest(
    string Name,
    string? AvatarUrl,
    string Strategy);

public sealed record AddBotToLeagueRequest(Guid BotId);
```

### 4.2 Commands

**CreateBotCommand.cs:**
```csharp
public sealed record CreateBotCommand(
    string Name,
    string? AvatarUrl,
    BotStrategy Strategy) : IRequest<Result<BotDto>>;
```

**AddBotToLeagueCommand.cs:**
```csharp
public sealed record AddBotToLeagueCommand(
    Guid LeagueId,
    Guid BotId) : IRequest<Result<LeagueBotDto>>;
```

**RemoveBotFromLeagueCommand.cs:**
```csharp
public sealed record RemoveBotFromLeagueCommand(
    Guid LeagueId,
    Guid BotId) : IRequest<Result>;
```

**PlaceBotBetsCommand.cs:**
```csharp
// Triggered by background service
public sealed record PlaceBotBetsCommand : IRequest<Result<int>>;
```

### 4.3 Queries

**GetBotsQuery.cs:**
```csharp
public sealed record GetBotsQuery : IRequest<Result<List<BotDto>>>;
```

**GetLeagueBotsQuery.cs:**
```csharp
public sealed record GetLeagueBotsQuery(Guid LeagueId) : IRequest<Result<List<LeagueBotDto>>>;
```

---

## Part 5: Infrastructure Layer - Background Service

### 5.1 Bot Betting Background Service

**BotBettingBackgroundService.cs:**
```csharp
public sealed class BotBettingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<BotBettingBackgroundService> logger) : BackgroundService
{
    // Run every 30 minutes during active hours (8:00-23:00 UTC)
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bot Betting Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Only run during active hours
                if (now.Hour >= 8 && now.Hour <= 23)
                {
                    await PlaceBotBetsAsync(stoppingToken);
                }
                else
                {
                    logger.LogDebug("Outside active hours, skipping bot betting run");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during bot betting run");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PlaceBotBetsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var botService = scope.ServiceProvider.GetRequiredService<IBotBettingService>();

        var betsPlaced = await botService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);

        if (betsPlaced > 0)
        {
            logger.LogInformation("Bot betting run completed: {BetsPlaced} bets placed", betsPlaced);
        }
    }
}
```

### 5.2 Dependency Injection Registration

**Update DependencyInjection.cs:**
```csharp
// In AddInfrastructureServices:
services.AddSingleton<BotStrategyFactory>();
services.AddScoped<IBotBettingService, BotBettingService>();
services.AddHostedService<BotBettingBackgroundService>();
```

---

## Part 6: API Layer - Endpoints

### 6.1 Bot Endpoints

**BotsEndpoints.cs:**
```csharp
public static class BotsEndpoints
{
    public static RouteGroupBuilder MapBotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bots")
            .WithTags("Bots");

        group.MapGet("/", GetBots)
            .WithName("GetBots")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetBots(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBotsQuery(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }
}
```

### 6.2 League Bot Management Endpoints

**Update LeaguesEndpoints.cs - Add bot management:**
```csharp
// GET /api/leagues/{id}/bots - Get bots in league
group.MapGet("/{id:guid}/bots", GetLeagueBots)
    .WithName("GetLeagueBots")
    .RequireAuthorization();

// POST /api/leagues/{id}/bots - Add bot to league
group.MapPost("/{id:guid}/bots", AddBotToLeague)
    .WithName("AddBotToLeague")
    .RequireAuthorization();

// DELETE /api/leagues/{id}/bots/{botId} - Remove bot from league
group.MapDelete("/{id:guid}/bots/{botId:guid}", RemoveBotFromLeague)
    .WithName("RemoveBotFromLeague")
    .RequireAuthorization();
```

### 6.3 Admin Bot Management Endpoints

**AdminBotsEndpoints.cs:**
```csharp
public static class AdminBotsEndpoints
{
    public static RouteGroupBuilder MapAdminBotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/bots")
            .WithTags("Admin - Bots")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/", CreateBot)
            .WithName("CreateBot");

        group.MapPost("/trigger-betting", TriggerBotBetting)
            .WithName("TriggerBotBetting");

        return group;
    }

    private static async Task<IResult> CreateBot(
        CreateBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BotStrategy>(request.Strategy, true, out var strategy))
        {
            return Results.BadRequest(new { error = "Invalid strategy" });
        }

        var command = new CreateBotCommand(request.Name, request.AvatarUrl, strategy);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/bots/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error });
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

---

## Part 7: API Endpoints Summary

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/bots` | Yes | List available bots |
| GET | `/api/leagues/{id}/bots` | Yes | Get bots in league |
| POST | `/api/leagues/{id}/bots` | Yes | Add bot to league (owner only) |
| DELETE | `/api/leagues/{id}/bots/{botId}` | Yes | Remove bot from league (owner only) |
| POST | `/api/admin/bots` | Admin | Create new bot |
| POST | `/api/admin/bots/trigger-betting` | Admin | Manually trigger bot betting |

---

## Part 8: Frontend Implementation

### 8.1 Types

**types/bot.ts:**
```typescript
export interface Bot {
  id: string;
  name: string;
  avatarUrl: string | null;
  strategy: BotStrategy;
  isActive: boolean;
  createdAt: string;
  lastBetPlacedAt: string | null;
}

export interface LeagueBot {
  id: string;
  name: string;
  avatarUrl: string | null;
  strategy: BotStrategy;
  addedAt: string;
}

export type BotStrategy =
  | 'Random'
  | 'HomeFavorer'
  | 'UnderdogSupporter'
  | 'DrawPredictor'
  | 'HighScorer';

export interface AddBotToLeagueRequest {
  botId: string;
}
```

### 8.2 API Hooks

**hooks/use-bots.ts:**
```typescript
export function useBots() {
  return useQuery({
    queryKey: ['bots'],
    queryFn: () => apiClient.get<Bot[]>('/bots'),
  });
}

export function useLeagueBots(leagueId: string) {
  return useQuery({
    queryKey: ['leagues', leagueId, 'bots'],
    queryFn: () => apiClient.get<LeagueBot[]>(`/leagues/${leagueId}/bots`),
  });
}

export function useAddBotToLeague(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (botId: string) =>
      apiClient.post<LeagueBot>(`/leagues/${leagueId}/bots`, { botId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'bots'] });
    },
  });
}

export function useRemoveBotFromLeague(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (botId: string) =>
      apiClient.delete(`/leagues/${leagueId}/bots/${botId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'bots'] });
    },
  });
}
```

### 8.3 Components

**components/bots/bot-indicator.tsx:**
```typescript
// Small badge/icon to indicate a user is a bot in leaderboards
interface BotIndicatorProps {
  strategy: BotStrategy;
  className?: string;
}

export function BotIndicator({ strategy, className }: BotIndicatorProps) {
  const strategyIcons: Record<BotStrategy, string> = {
    Random: '🎲',
    HomeFavorer: '🏠',
    UnderdogSupporter: '🐕',
    DrawPredictor: '🤝',
    HighScorer: '⚽',
  };

  return (
    <span
      className={cn("text-xs bg-muted px-1.5 py-0.5 rounded-full", className)}
      title={`Bot: ${strategy}`}
    >
      {strategyIcons[strategy]} Bot
    </span>
  );
}
```

**components/bots/add-bot-modal.tsx:**
```typescript
// Modal to select and add bots to a league
interface AddBotModalProps {
  leagueId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function AddBotModal({ leagueId, open, onOpenChange }: AddBotModalProps) {
  const { data: bots, isLoading } = useBots();
  const { data: leagueBots } = useLeagueBots(leagueId);
  const addBot = useAddBotToLeague(leagueId);

  const availableBots = bots?.filter(
    bot => !leagueBots?.some(lb => lb.id === bot.id)
  ) ?? [];

  const handleAddBot = async (botId: string) => {
    await addBot.mutateAsync(botId);
    toast.success('Bot added to league');
  };

  // Render modal with bot selection cards...
}
```

**components/bots/league-bots-list.tsx:**
```typescript
// List of bots in a league with remove option
interface LeagueBotsListProps {
  leagueId: string;
  isOwner: boolean;
}

export function LeagueBotsList({ leagueId, isOwner }: LeagueBotsListProps) {
  const { data: bots, isLoading } = useLeagueBots(leagueId);
  const removeBot = useRemoveBotFromLeague(leagueId);

  // Render list with bot cards and remove buttons...
}
```

### 8.4 Update Existing Components

**Update leaderboard.tsx:**
- Check if user `isBot` flag is true
- Display `<BotIndicator>` next to bot names
- Use bot's strategy-specific avatar or indicator

**Update league-form.tsx:**
- Add "Enable Bots" toggle switch
- When enabled, show "Add Bots" button that opens `<AddBotModal>`

**Update league-detail.tsx:**
- Add "Bots" tab showing `<LeagueBotsList>`
- Allow owner to add/remove bots

---

## Part 9: Seed Data - Default Bots

### 9.1 Bot Seeding

Create 5 default bots on first run:

| Name | Strategy | Avatar | Description |
|------|----------|--------|-------------|
| Lucky Larry | Random | 🎲 | Makes random predictions |
| Home Hero | HomeFavorer | 🏠 | Always backs the home team |
| Underdog Dave | UnderdogSupporter | 🐕 | Loves an upset |
| Draw Dan | DrawPredictor | 🤝 | Expects stalemates |
| Goal Gary | HighScorer | ⚽ | Predicts high-scoring games |

**BotSeeder.cs:**
```csharp
public sealed class BotSeeder(IApplicationDbContext context)
{
    public async Task SeedDefaultBotsAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Bots.AnyAsync(cancellationToken))
            return;

        var bots = new[]
        {
            CreateBot("Lucky Larry", BotStrategy.Random, "🎲"),
            CreateBot("Home Hero", BotStrategy.HomeFavorer, "🏠"),
            CreateBot("Underdog Dave", BotStrategy.UnderdogSupporter, "🐕"),
            CreateBot("Draw Dan", BotStrategy.DrawPredictor, "🤝"),
            CreateBot("Goal Gary", BotStrategy.HighScorer, "⚽"),
        };

        foreach (var (user, bot) in bots)
        {
            context.Users.Add(user);
            context.Bots.Add(bot);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private (User user, Bot bot) CreateBot(string name, BotStrategy strategy, string avatarEmoji)
    {
        var email = $"bot_{name.ToLower().Replace(" ", "_")}@extratime.local";
        var user = User.Register(email, name, BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()));
        user.MarkAsBot();

        var bot = new Bot
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = name,
            AvatarUrl = null, // Use emoji in frontend
            Strategy = strategy,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return (user, bot);
    }
}
```

---

## Part 10: Implementation Checklist

### Phase 7A: Domain Layer
- [ ] Create `BotStrategy` enum
- [ ] Create `Bot` entity
- [ ] Create `LeagueBotMember` entity
- [ ] Update `User` entity with `IsBot` flag
- [ ] Update `League` entity with `BotsEnabled` and bot navigation

### Phase 7B: Infrastructure Layer
- [ ] Create `BotConfiguration`
- [ ] Create `LeagueBotMemberConfiguration`
- [ ] Update `ApplicationDbContext` with new DbSets
- [ ] Create database migration
- [ ] Run migration

### Phase 7C: Application Layer - Strategies
- [ ] Create `IBotBettingStrategy` interface
- [ ] Implement `RandomStrategy`
- [ ] Implement `HomeFavorerStrategy`
- [ ] Implement `UnderdogSupporterStrategy`
- [ ] Implement `DrawPredictorStrategy`
- [ ] Implement `HighScorerStrategy`
- [ ] Create `BotStrategyFactory`

### Phase 7D: Application Layer - Services
- [ ] Create DTOs in `BotDtos.cs`
- [ ] Create `IBotBettingService` interface
- [ ] Implement `BotBettingService`
- [ ] Create `CreateBotCommand` + Handler
- [ ] Create `AddBotToLeagueCommand` + Handler
- [ ] Create `RemoveBotFromLeagueCommand` + Handler
- [ ] Create `GetBotsQuery` + Handler
- [ ] Create `GetLeagueBotsQuery` + Handler

### Phase 7E: Infrastructure - Background Service
- [ ] Create `BotBettingBackgroundService`
- [ ] Register services in DI

### Phase 7F: API Layer
- [ ] Create `BotsEndpoints`
- [ ] Create `AdminBotsEndpoints`
- [ ] Update `LeaguesEndpoints` with bot management
- [ ] Register endpoints in Program.cs

### Phase 7G: Seed Data
- [ ] Create `BotSeeder`
- [ ] Add seeding to application startup
- [ ] Verify 5 default bots created

### Phase 7H: Frontend - Types & Hooks
- [ ] Create `types/bot.ts`
- [ ] Create `hooks/use-bots.ts`

### Phase 7I: Frontend - Components
- [ ] Create `bot-indicator.tsx`
- [ ] Create `add-bot-modal.tsx`
- [ ] Create `league-bots-list.tsx`
- [ ] Create `bot-avatar.tsx`

### Phase 7J: Frontend - Integration
- [ ] Update leaderboard to show bot indicator
- [ ] Update league form with bots toggle
- [ ] Update league detail with bots tab
- [ ] Test bot betting flow end-to-end

### Phase 7K: Testing & Verification
- [ ] Verify bots place bets before deadlines
- [ ] Verify bot bets are scored correctly
- [ ] Verify bots appear in leaderboards
- [ ] Verify league owners can add/remove bots
- [ ] Test all 5 strategy types

---

## Notes

### Future Enhancements (Not in MVP)
- **Advanced Stats Strategy**: Use historical match data for smarter predictions
- **Adaptive Strategy**: Bots learn from their success/failure
- **Bot Personalities**: Different comment styles for each bot
- **Bot Challenges**: Users can challenge specific bots

### Azure Functions (Phase 8)
The `BotBettingBackgroundService` can be replaced with an Azure Function Timer trigger:
```csharp
[Function("PlaceBotBets")]
public async Task Run([TimerTrigger("0 */30 8-23 * * *")] TimerInfo timer)
{
    // Run every 30 minutes between 8am and 11pm
    await _botService.PlaceBetsForUpcomingMatchesAsync();
}
```
