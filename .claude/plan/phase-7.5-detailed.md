# Phase 7.5: Intelligent Stats-Based Bots - Detailed Implementation Plan ✅ COMPLETE

## Overview
Add intelligent bots that analyze team performance statistics to make data-driven predictions. These bots use a configurable `StatsAnalyst` strategy that can be tuned to create different personalities (Form Analyst, Defensive Expert, etc.).

> **Prerequisite**: Phase 7 (Basic Bot System) must be complete
> **Future Enhancement**: Phase 9 (Extended Football Data) will add standings, H2H, and scorer data for even smarter predictions

---

## Part 1: Design Philosophy

### 1.1 Single Strategy, Multiple Personalities

Instead of creating separate strategy classes, we use one `StatsAnalyst` strategy with a JSON configuration that controls:
- Which factors to analyze
- How to weight each factor
- Prediction style (conservative vs aggressive)

```
┌─────────────────────────────────────────────────────────────┐
│                    StatsAnalyst Strategy                      │
├─────────────────────────────────────────────────────────────┤
│  Configuration Options:                                       │
│  ├── formWeight: 0.0 - 1.0 (how much recent form matters)    │
│  ├── homeAdvantageWeight: 0.0 - 1.0 (home field bonus)       │
│  ├── goalTrendWeight: 0.0 - 1.0 (scoring/conceding trends)   │
│  ├── matchesAnalyzed: 3 - 10 (how many past matches)         │
│  ├── highStakesBoost: true/false (adjust for big games)      │
│  ├── predictionStyle: "conservative" | "moderate" | "bold"   │
│  └── randomVariance: 0.0 - 0.3 (adds unpredictability)       │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 Bot Personalities from Configuration

| Bot Name | Focus | Configuration |
|----------|-------|---------------|
| Stats Genius | All-rounder | Balanced weights, moderate style |
| Form Master | Recent results | High formWeight, low others |
| Fortress Fred | Home advantage | High homeAdvantageWeight |
| Goal Hunter | High-scoring | High goalTrendWeight, bold style |
| Safe Steve | Conservative | Low variance, conservative style |
| Chaos Carl | Unpredictable | High randomVariance, bold style |

---

## Part 2: Domain Layer

### 2.1 New Enum Value

**Update BotStrategy.cs:**
```csharp
public enum BotStrategy
{
    Random = 0,
    HomeFavorer = 1,
    UnderdogSupporter = 2,
    DrawPredictor = 3,
    HighScorer = 4,
    StatsAnalyst = 10    // New - configurable stats-based strategy
}
```

### 2.2 Team Form Cache Entity

Create a cached representation of team form to avoid recalculating on every prediction:

**TeamFormCache.cs:**
```csharp
namespace ExtraTime.Domain.Entities;

public sealed class TeamFormCache : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    // Form stats (last N matches)
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }

    // Home-specific stats
    public int HomeMatchesPlayed { get; set; }
    public int HomeWins { get; set; }
    public int HomeGoalsScored { get; set; }
    public int HomeGoalsConceded { get; set; }

    // Away-specific stats
    public int AwayMatchesPlayed { get; set; }
    public int AwayWins { get; set; }
    public int AwayGoalsScored { get; set; }
    public int AwayGoalsConceded { get; set; }

    // Computed metrics
    public double PointsPerMatch { get; set; }      // (Wins*3 + Draws) / Matches
    public double GoalsPerMatch { get; set; }       // GoalsScored / Matches
    public double GoalsConcededPerMatch { get; set; }
    public double HomeWinRate { get; set; }         // HomeWins / HomeMatches
    public double AwayWinRate { get; set; }

    // Streak tracking
    public int CurrentStreak { get; set; }          // Positive = wins, negative = losses, 0 = draw
    public string RecentForm { get; set; } = "";    // e.g., "WWDLW" last 5 results

    // Metadata
    public int MatchesAnalyzed { get; set; }        // How many matches used for calculation
    public DateTime CalculatedAt { get; set; }
    public DateTime? LastMatchDate { get; set; }

    // Methods
    public double GetFormScore()
    {
        // Returns 0-100 score based on recent form
        if (MatchesPlayed == 0) return 50.0;
        return (PointsPerMatch / 3.0) * 100;
    }

    public double GetHomeStrength()
    {
        if (HomeMatchesPlayed == 0) return 0.5;
        return HomeWinRate;
    }

    public double GetAwayStrength()
    {
        if (AwayMatchesPlayed == 0) return 0.3;
        return AwayWinRate;
    }

    public double GetAttackStrength()
    {
        if (MatchesPlayed == 0) return 1.5;
        return GoalsPerMatch;
    }

    public double GetDefenseStrength()
    {
        if (MatchesPlayed == 0) return 1.5;
        return GoalsConcededPerMatch;
    }
}
```

### 2.3 Stats Analyst Configuration Model

**StatsAnalystConfig.cs:**
```csharp
namespace ExtraTime.Domain.ValueObjects;

public sealed record StatsAnalystConfig
{
    // Weight factors (0.0 - 1.0, should sum to ~1.0 for balanced prediction)
    public double FormWeight { get; init; } = 0.35;
    public double HomeAdvantageWeight { get; init; } = 0.25;
    public double GoalTrendWeight { get; init; } = 0.25;
    public double StreakWeight { get; init; } = 0.15;

    // Analysis parameters
    public int MatchesAnalyzed { get; init; } = 5;
    public bool HighStakesBoost { get; init; } = true;
    public int LateSeasonMatchday { get; init; } = 30; // Matchday when "late season" starts

    // Prediction style
    public PredictionStyle Style { get; init; } = PredictionStyle.Moderate;
    public double RandomVariance { get; init; } = 0.1;

    // Goal prediction bounds based on style
    public int MinGoals => Style switch
    {
        PredictionStyle.Conservative => 0,
        PredictionStyle.Moderate => 0,
        PredictionStyle.Bold => 1,
        _ => 0
    };

    public int MaxGoals => Style switch
    {
        PredictionStyle.Conservative => 2,
        PredictionStyle.Moderate => 4,
        PredictionStyle.Bold => 5,
        _ => 4
    };

    // JSON serialization helper
    public string ToJson() => JsonSerializer.Serialize(this);

    public static StatsAnalystConfig FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new StatsAnalystConfig();
        try
        {
            return JsonSerializer.Deserialize<StatsAnalystConfig>(json) ?? new StatsAnalystConfig();
        }
        catch
        {
            return new StatsAnalystConfig();
        }
    }

    // Preset configurations
    public static StatsAnalystConfig Balanced => new();

    public static StatsAnalystConfig FormFocused => new()
    {
        FormWeight = 0.60,
        HomeAdvantageWeight = 0.15,
        GoalTrendWeight = 0.15,
        StreakWeight = 0.10,
        MatchesAnalyzed = 5
    };

    public static StatsAnalystConfig HomeAdvantage => new()
    {
        FormWeight = 0.20,
        HomeAdvantageWeight = 0.50,
        GoalTrendWeight = 0.20,
        StreakWeight = 0.10
    };

    public static StatsAnalystConfig GoalFocused => new()
    {
        FormWeight = 0.25,
        HomeAdvantageWeight = 0.15,
        GoalTrendWeight = 0.50,
        StreakWeight = 0.10,
        Style = PredictionStyle.Bold
    };

    public static StatsAnalystConfig Conservative => new()
    {
        Style = PredictionStyle.Conservative,
        RandomVariance = 0.05
    };

    public static StatsAnalystConfig Chaotic => new()
    {
        RandomVariance = 0.30,
        Style = PredictionStyle.Bold
    };
}

public enum PredictionStyle
{
    Conservative = 0,  // Lower scores, fewer upsets
    Moderate = 1,      // Balanced predictions
    Bold = 2           // Higher scores, more upsets
}
```

---

## Part 3: Infrastructure Layer

### 3.1 Team Form Cache Configuration

**TeamFormCacheConfiguration.cs:**
```csharp
public sealed class TeamFormCacheConfiguration : IEntityTypeConfiguration<TeamFormCache>
{
    public void Configure(EntityTypeBuilder<TeamFormCache> builder)
    {
        builder.ToTable("TeamFormCaches");

        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Competition)
            .WithMany()
            .HasForeignKey(t => t.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.RecentForm)
            .HasMaxLength(20);

        builder.HasIndex(t => new { t.TeamId, t.CompetitionId })
            .IsUnique();

        builder.HasIndex(t => t.CalculatedAt);
    }
}
```

### 3.2 Update ApplicationDbContext

```csharp
public DbSet<TeamFormCache> TeamFormCaches => Set<TeamFormCache>();
```

### 3.3 Migration

Create migration: `AddTeamFormCache`
- Creates `TeamFormCaches` table with all form statistics
- Adds unique index on (TeamId, CompetitionId)

---

## Part 4: Application Layer - Form Calculator Service

### 4.1 Interface

**ITeamFormCalculator.cs:**
```csharp
public interface ITeamFormCalculator
{
    Task<TeamFormCache> CalculateFormAsync(
        Guid teamId,
        Guid competitionId,
        int matchesAnalyzed = 5,
        CancellationToken cancellationToken = default);

    Task RefreshAllFormCachesAsync(CancellationToken cancellationToken = default);

    Task<TeamFormCache?> GetCachedFormAsync(
        Guid teamId,
        Guid competitionId,
        CancellationToken cancellationToken = default);
}
```

### 4.2 Implementation

**TeamFormCalculator.cs:**
```csharp
public sealed class TeamFormCalculator(
    IApplicationDbContext context,
    TimeProvider timeProvider,
    ILogger<TeamFormCalculator> logger) : ITeamFormCalculator
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(6);

    public async Task<TeamFormCache> CalculateFormAsync(
        Guid teamId,
        Guid competitionId,
        int matchesAnalyzed = 5,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Check for fresh cache
        var cached = await GetCachedFormAsync(teamId, competitionId, cancellationToken);
        if (cached != null && (now - cached.CalculatedAt) < CacheExpiry)
        {
            return cached;
        }

        // Get recent finished matches for this team in this competition
        var matches = await context.Matches
            .Where(m => m.CompetitionId == competitionId)
            .Where(m => m.Status == MatchStatus.Finished)
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .OrderByDescending(m => m.MatchDateUtc)
            .Take(matchesAnalyzed)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
        {
            // Return default form for teams with no history
            return CreateDefaultForm(teamId, competitionId, matchesAnalyzed, now);
        }

        // Calculate stats
        var form = CalculateStats(teamId, competitionId, matches, matchesAnalyzed, now);

        // Upsert cache
        if (cached != null)
        {
            UpdateFormCache(cached, form);
        }
        else
        {
            context.TeamFormCaches.Add(form);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Calculated form for team {TeamId}: {Form} ({Points} PPM)",
            teamId, form.RecentForm, form.PointsPerMatch);

        return form;
    }

    private TeamFormCache CalculateStats(
        Guid teamId,
        Guid competitionId,
        List<Match> matches,
        int matchesAnalyzed,
        DateTime now)
    {
        var form = new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            CompetitionId = competitionId,
            MatchesAnalyzed = matchesAnalyzed,
            CalculatedAt = now,
            LastMatchDate = matches.FirstOrDefault()?.MatchDateUtc
        };

        var recentFormBuilder = new StringBuilder();
        int currentStreak = 0;
        bool streakType = true; // true = wins

        foreach (var match in matches)
        {
            bool isHome = match.HomeTeamId == teamId;
            int goalsFor = isHome ? match.HomeScore!.Value : match.AwayScore!.Value;
            int goalsAgainst = isHome ? match.AwayScore!.Value : match.HomeScore!.Value;

            form.MatchesPlayed++;
            form.GoalsScored += goalsFor;
            form.GoalsConceded += goalsAgainst;

            if (isHome)
            {
                form.HomeMatchesPlayed++;
                form.HomeGoalsScored += goalsFor;
                form.HomeGoalsConceded += goalsAgainst;
            }
            else
            {
                form.AwayMatchesPlayed++;
                form.AwayGoalsScored += goalsFor;
                form.AwayGoalsConceded += goalsAgainst;
            }

            // Determine result
            char result;
            if (goalsFor > goalsAgainst)
            {
                form.Wins++;
                if (isHome) form.HomeWins++;
                else form.AwayWins++;
                result = 'W';

                if (recentFormBuilder.Length == 0) { currentStreak = 1; streakType = true; }
                else if (streakType) currentStreak++;
            }
            else if (goalsFor < goalsAgainst)
            {
                form.Losses++;
                result = 'L';

                if (recentFormBuilder.Length == 0) { currentStreak = -1; streakType = false; }
                else if (!streakType) currentStreak--;
            }
            else
            {
                form.Draws++;
                result = 'D';
                currentStreak = 0;
            }

            recentFormBuilder.Append(result);
        }

        // Calculate derived metrics
        if (form.MatchesPlayed > 0)
        {
            form.PointsPerMatch = (form.Wins * 3.0 + form.Draws) / form.MatchesPlayed;
            form.GoalsPerMatch = (double)form.GoalsScored / form.MatchesPlayed;
            form.GoalsConcededPerMatch = (double)form.GoalsConceded / form.MatchesPlayed;
        }

        if (form.HomeMatchesPlayed > 0)
        {
            form.HomeWinRate = (double)form.HomeWins / form.HomeMatchesPlayed;
        }

        if (form.AwayMatchesPlayed > 0)
        {
            form.AwayWinRate = (double)form.AwayWins / form.AwayMatchesPlayed;
        }

        form.CurrentStreak = currentStreak;
        form.RecentForm = recentFormBuilder.ToString();

        return form;
    }

    private TeamFormCache CreateDefaultForm(
        Guid teamId,
        Guid competitionId,
        int matchesAnalyzed,
        DateTime now)
    {
        return new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            CompetitionId = competitionId,
            MatchesAnalyzed = matchesAnalyzed,
            MatchesPlayed = 0,
            PointsPerMatch = 1.0, // Average assumption
            GoalsPerMatch = 1.5,
            GoalsConcededPerMatch = 1.5,
            HomeWinRate = 0.45,   // Slight home advantage
            AwayWinRate = 0.30,
            RecentForm = "",
            CalculatedAt = now
        };
    }

    private void UpdateFormCache(TeamFormCache existing, TeamFormCache updated)
    {
        existing.MatchesPlayed = updated.MatchesPlayed;
        existing.Wins = updated.Wins;
        existing.Draws = updated.Draws;
        existing.Losses = updated.Losses;
        existing.GoalsScored = updated.GoalsScored;
        existing.GoalsConceded = updated.GoalsConceded;
        existing.HomeMatchesPlayed = updated.HomeMatchesPlayed;
        existing.HomeWins = updated.HomeWins;
        existing.HomeGoalsScored = updated.HomeGoalsScored;
        existing.HomeGoalsConceded = updated.HomeGoalsConceded;
        existing.AwayMatchesPlayed = updated.AwayMatchesPlayed;
        existing.AwayWins = updated.AwayWins;
        existing.AwayGoalsScored = updated.AwayGoalsScored;
        existing.AwayGoalsConceded = updated.AwayGoalsConceded;
        existing.PointsPerMatch = updated.PointsPerMatch;
        existing.GoalsPerMatch = updated.GoalsPerMatch;
        existing.GoalsConcededPerMatch = updated.GoalsConcededPerMatch;
        existing.HomeWinRate = updated.HomeWinRate;
        existing.AwayWinRate = updated.AwayWinRate;
        existing.CurrentStreak = updated.CurrentStreak;
        existing.RecentForm = updated.RecentForm;
        existing.MatchesAnalyzed = updated.MatchesAnalyzed;
        existing.CalculatedAt = updated.CalculatedAt;
        existing.LastMatchDate = updated.LastMatchDate;
    }

    public async Task<TeamFormCache?> GetCachedFormAsync(
        Guid teamId,
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamFormCaches
            .FirstOrDefaultAsync(
                t => t.TeamId == teamId && t.CompetitionId == competitionId,
                cancellationToken);
    }

    public async Task RefreshAllFormCachesAsync(CancellationToken cancellationToken = default)
    {
        // Get all teams that have played matches
        var teamCompetitions = await context.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Select(m => new { m.HomeTeamId, m.CompetitionId })
            .Union(context.Matches
                .Where(m => m.Status == MatchStatus.Finished)
                .Select(m => new { HomeTeamId = m.AwayTeamId, m.CompetitionId }))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var tc in teamCompetitions)
        {
            await CalculateFormAsync(tc.HomeTeamId, tc.CompetitionId, 5, cancellationToken);
        }

        logger.LogInformation("Refreshed form caches for {Count} team-competition pairs", teamCompetitions.Count);
    }
}
```

---

## Part 5: Application Layer - Stats Analyst Strategy

### 5.1 Match Analysis Model

**MatchAnalysis.cs:**
```csharp
public sealed record MatchAnalysis
{
    public required Match Match { get; init; }
    public required TeamFormCache HomeTeamForm { get; init; }
    public required TeamFormCache AwayTeamForm { get; init; }

    // Calculated factors
    public double HomeFormScore { get; init; }
    public double AwayFormScore { get; init; }
    public double HomeAdvantage { get; init; }
    public double ExpectedHomeGoals { get; init; }
    public double ExpectedAwayGoals { get; init; }
    public bool IsHighStakes { get; init; }
    public bool IsLateSeasonMatch { get; init; }

    // Final prediction
    public int PredictedHomeScore { get; init; }
    public int PredictedAwayScore { get; init; }
}
```

### 5.2 Stats Analyst Strategy Implementation

**StatsAnalystStrategy.cs:**
```csharp
public sealed class StatsAnalystStrategy : IBotBettingStrategy
{
    private readonly ITeamFormCalculator _formCalculator;
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.StatsAnalyst;

    public StatsAnalystStrategy(ITeamFormCalculator formCalculator)
    {
        _formCalculator = formCalculator;
    }

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        var config = StatsAnalystConfig.FromJson(configuration);

        // This is a sync wrapper - the actual implementation needs async
        // In practice, form should be pre-calculated/cached
        var homeForm = GetFormSync(match.HomeTeamId, match.CompetitionId, config.MatchesAnalyzed);
        var awayForm = GetFormSync(match.AwayTeamId, match.CompetitionId, config.MatchesAnalyzed);

        var analysis = AnalyzeMatch(match, homeForm, awayForm, config);

        return (analysis.PredictedHomeScore, analysis.PredictedAwayScore);
    }

    // Async version for service usage
    public async Task<(int HomeScore, int AwayScore)> GeneratePredictionAsync(
        Match match,
        string? configuration,
        CancellationToken cancellationToken = default)
    {
        var config = StatsAnalystConfig.FromJson(configuration);

        var homeForm = await _formCalculator.CalculateFormAsync(
            match.HomeTeamId, match.CompetitionId, config.MatchesAnalyzed, cancellationToken);
        var awayForm = await _formCalculator.CalculateFormAsync(
            match.AwayTeamId, match.CompetitionId, config.MatchesAnalyzed, cancellationToken);

        var analysis = AnalyzeMatch(match, homeForm, awayForm, config);

        return (analysis.PredictedHomeScore, analysis.PredictedAwayScore);
    }

    private MatchAnalysis AnalyzeMatch(
        Match match,
        TeamFormCache homeForm,
        TeamFormCache awayForm,
        StatsAnalystConfig config)
    {
        // Step 1: Calculate form scores (0-100)
        double homeFormScore = homeForm.GetFormScore() * config.FormWeight;
        double awayFormScore = awayForm.GetFormScore() * config.FormWeight;

        // Step 2: Apply home advantage
        double homeAdvantage = homeForm.GetHomeStrength() * config.HomeAdvantageWeight * 100;
        double awayPenalty = (1 - awayForm.GetAwayStrength()) * config.HomeAdvantageWeight * 50;

        // Step 3: Calculate expected goals from trends
        double expectedHomeGoals = (homeForm.GetAttackStrength() + (1 / Math.Max(0.5, awayForm.GetDefenseStrength()))) / 2;
        double expectedAwayGoals = (awayForm.GetAttackStrength() + (1 / Math.Max(0.5, homeForm.GetDefenseStrength()))) / 2;

        // Apply goal trend weight
        expectedHomeGoals *= (1 + config.GoalTrendWeight);
        expectedAwayGoals *= (1 + config.GoalTrendWeight * 0.8); // Away teams score slightly less

        // Step 4: Apply streak influence
        if (config.StreakWeight > 0)
        {
            double homeStreakBonus = homeForm.CurrentStreak * 0.1 * config.StreakWeight;
            double awayStreakBonus = awayForm.CurrentStreak * 0.1 * config.StreakWeight;
            expectedHomeGoals += homeStreakBonus;
            expectedAwayGoals += awayStreakBonus;
        }

        // Step 5: Detect high stakes match
        bool isLateSeasonMatch = match.Matchday >= config.LateSeasonMatchday;
        bool isHighStakes = isLateSeasonMatch; // Can extend with standings data in Phase 9

        if (isHighStakes && config.HighStakesBoost)
        {
            // High stakes matches tend to be tighter
            double avgGoals = (expectedHomeGoals + expectedAwayGoals) / 2;
            expectedHomeGoals = expectedHomeGoals * 0.85 + avgGoals * 0.15;
            expectedAwayGoals = expectedAwayGoals * 0.85 + avgGoals * 0.15;
        }

        // Step 6: Apply home advantage to final prediction
        double homeFinalScore = homeFormScore + homeAdvantage - awayPenalty;
        double awayFinalScore = awayFormScore - homeAdvantage * 0.5;

        // Step 7: Convert to goal predictions
        int predictedHomeGoals = ConvertToGoals(expectedHomeGoals, homeFinalScore, config);
        int predictedAwayGoals = ConvertToGoals(expectedAwayGoals, awayFinalScore, config);

        // Step 8: Apply random variance
        if (config.RandomVariance > 0)
        {
            predictedHomeGoals = ApplyVariance(predictedHomeGoals, config);
            predictedAwayGoals = ApplyVariance(predictedAwayGoals, config);
        }

        // Step 9: Clamp to valid range
        predictedHomeGoals = Math.Clamp(predictedHomeGoals, config.MinGoals, config.MaxGoals);
        predictedAwayGoals = Math.Clamp(predictedAwayGoals, config.MinGoals, config.MaxGoals);

        return new MatchAnalysis
        {
            Match = match,
            HomeTeamForm = homeForm,
            AwayTeamForm = awayForm,
            HomeFormScore = homeFormScore,
            AwayFormScore = awayFormScore,
            HomeAdvantage = homeAdvantage,
            ExpectedHomeGoals = expectedHomeGoals,
            ExpectedAwayGoals = expectedAwayGoals,
            IsHighStakes = isHighStakes,
            IsLateSeasonMatch = isLateSeasonMatch,
            PredictedHomeScore = predictedHomeGoals,
            PredictedAwayScore = predictedAwayGoals
        };
    }

    private int ConvertToGoals(double expectedGoals, double formScore, StatsAnalystConfig config)
    {
        // Combine expected goals with form-based adjustment
        double adjustedGoals = expectedGoals;

        // Form score influences goal expectation
        if (formScore > 50)
        {
            adjustedGoals *= 1 + ((formScore - 50) / 200); // Up to +25% for great form
        }
        else
        {
            adjustedGoals *= 1 - ((50 - formScore) / 200); // Down to -25% for poor form
        }

        // Style-based rounding
        return config.Style switch
        {
            PredictionStyle.Conservative => (int)Math.Floor(adjustedGoals),
            PredictionStyle.Bold => (int)Math.Ceiling(adjustedGoals),
            _ => (int)Math.Round(adjustedGoals)
        };
    }

    private int ApplyVariance(int goals, StatsAnalystConfig config)
    {
        double variance = (_random.NextDouble() - 0.5) * 2 * config.RandomVariance;
        int adjustment = (int)Math.Round(variance * 2);
        return goals + adjustment;
    }

    // Sync helper for interface compliance (uses cached data)
    private TeamFormCache GetFormSync(Guid teamId, Guid competitionId, int matchesAnalyzed)
    {
        // In a real implementation, this would need to be pre-cached
        // or the interface needs to support async
        return new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            CompetitionId = competitionId,
            MatchesAnalyzed = matchesAnalyzed,
            PointsPerMatch = 1.5, // Default assumption
            GoalsPerMatch = 1.5,
            GoalsConcededPerMatch = 1.2,
            HomeWinRate = 0.45,
            AwayWinRate = 0.30,
            RecentForm = "",
            CalculatedAt = DateTime.UtcNow
        };
    }
}
```

### 5.3 Update Strategy Factory

**BotStrategyFactory.cs (updated):**
```csharp
public sealed class BotStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<BotStrategy, Func<IBotBettingStrategy>> _factories;

    public BotStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _factories = new Dictionary<BotStrategy, Func<IBotBettingStrategy>>
        {
            { BotStrategy.Random, () => new RandomStrategy() },
            { BotStrategy.HomeFavorer, () => new HomeFavorerStrategy() },
            { BotStrategy.UnderdogSupporter, () => new UnderdogSupporterStrategy() },
            { BotStrategy.DrawPredictor, () => new DrawPredictorStrategy() },
            { BotStrategy.HighScorer, () => new HighScorerStrategy() },
            { BotStrategy.StatsAnalyst, () => new StatsAnalystStrategy(
                _serviceProvider.GetRequiredService<ITeamFormCalculator>()) }
        };
    }

    public IBotBettingStrategy GetStrategy(BotStrategy strategy)
    {
        return _factories.TryGetValue(strategy, out var factory)
            ? factory()
            : _factories[BotStrategy.Random]();
    }
}
```

---

## Part 6: Updated Bot Betting Service

### 6.1 Async-Aware Betting Service

**BotBettingService.cs (updated for StatsAnalyst):**
```csharp
public sealed class BotBettingService(
    IApplicationDbContext context,
    BotStrategyFactory strategyFactory,
    ITeamFormCalculator formCalculator,
    TimeProvider timeProvider,
    ILogger<BotBettingService> logger) : IBotBettingService
{
    public async Task<int> PlaceBetsForUpcomingMatchesAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoffTime = now.AddHours(24);

        // Refresh form caches first for StatsAnalyst bots
        await formCalculator.RefreshAllFormCachesAsync(cancellationToken);

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
                var existingBet = await context.Bets
                    .FirstOrDefaultAsync(b =>
                        b.LeagueId == league.Id &&
                        b.UserId == bot.UserId &&
                        b.MatchId == match.Id,
                        cancellationToken);

                if (existingBet != null) continue;
                if (!match.IsOpenForBetting(league.BettingDeadlineMinutes, now)) continue;

                // Use async prediction for StatsAnalyst
                int homeScore, awayScore;
                if (strategy is StatsAnalystStrategy statsStrategy)
                {
                    (homeScore, awayScore) = await statsStrategy.GeneratePredictionAsync(
                        match, bot.Configuration, cancellationToken);
                }
                else
                {
                    (homeScore, awayScore) = strategy.GeneratePrediction(match, bot.Configuration);
                }

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
                    "Bot {BotName} ({Strategy}) placed bet {HomeScore}-{AwayScore} for match {MatchId}",
                    bot.Name, bot.Strategy, homeScore, awayScore, match.Id);
            }

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

## Part 7: Form Cache Refresh Background Service

### 7.1 Form Cache Background Service

**FormCacheBackgroundService.cs:**
```csharp
public sealed class FormCacheBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<FormCacheBackgroundService> logger) : BackgroundService
{
    // Refresh form caches every 4 hours
    private static readonly TimeSpan Interval = TimeSpan.FromHours(4);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Form Cache Service started");

        // Initial refresh on startup
        await RefreshCachesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);

            try
            {
                await RefreshCachesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing form caches");
            }
        }
    }

    private async Task RefreshCachesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var formCalculator = scope.ServiceProvider.GetRequiredService<ITeamFormCalculator>();

        await formCalculator.RefreshAllFormCachesAsync(cancellationToken);
        logger.LogInformation("Form caches refreshed");
    }
}
```

---

## Part 8: Seed Data - Intelligent Bots

### 8.1 Stats Analyst Bot Seeds

Add these to `BotSeeder.cs`:

| Bot Name | Strategy | Configuration | Description |
|----------|----------|---------------|-------------|
| Stats Genius | StatsAnalyst | Balanced preset | All-round statistical analysis |
| Form Master | StatsAnalyst | FormFocused preset | Heavily weights recent results |
| Fortress Fred | StatsAnalyst | HomeAdvantage preset | Believes home teams always win |
| Goal Hunter | StatsAnalyst | GoalFocused preset | Predicts high-scoring games |
| Safe Steve | StatsAnalyst | Conservative preset | Low-risk, low-score predictions |
| Chaos Carl | StatsAnalyst | Chaotic preset | Unpredictable wild predictions |

**Updated BotSeeder.cs:**
```csharp
public async Task SeedDefaultBotsAsync(CancellationToken cancellationToken = default)
{
    if (await context.Bots.AnyAsync(cancellationToken))
        return;

    var bots = new[]
    {
        // Phase 7 - Basic bots
        CreateBot("Lucky Larry", BotStrategy.Random, null, "🎲"),
        CreateBot("Home Hero", BotStrategy.HomeFavorer, null, "🏠"),
        CreateBot("Underdog Dave", BotStrategy.UnderdogSupporter, null, "🐕"),
        CreateBot("Draw Dan", BotStrategy.DrawPredictor, null, "🤝"),
        CreateBot("Goal Gary", BotStrategy.HighScorer, null, "⚽"),

        // Phase 7.5 - Intelligent bots
        CreateBot("Stats Genius", BotStrategy.StatsAnalyst,
            StatsAnalystConfig.Balanced.ToJson(), "🧠"),
        CreateBot("Form Master", BotStrategy.StatsAnalyst,
            StatsAnalystConfig.FormFocused.ToJson(), "📈"),
        CreateBot("Fortress Fred", BotStrategy.StatsAnalyst,
            StatsAnalystConfig.HomeAdvantage.ToJson(), "🏰"),
        CreateBot("Goal Hunter", BotStrategy.StatsAnalyst,
            StatsAnalystConfig.GoalFocused.ToJson(), "🎯"),
        CreateBot("Safe Steve", BotStrategy.StatsAnalyst,
            StatsAnalystConfig.Conservative.ToJson(), "🛡️"),
        CreateBot("Chaos Carl", BotStrategy.StatsAnalyst,
            StatsAnalystConfig.Chaotic.ToJson(), "🌪️"),
    };

    foreach (var (user, bot) in bots)
    {
        context.Users.Add(user);
        context.Bots.Add(bot);
    }

    await context.SaveChangesAsync(cancellationToken);
    logger.LogInformation("Seeded {Count} bots", bots.Length);
}

private (User user, Bot bot) CreateBot(
    string name,
    BotStrategy strategy,
    string? configuration,
    string avatarEmoji)
{
    var email = $"bot_{name.ToLower().Replace(" ", "_")}@extratime.local";
    var user = User.Register(email, name, BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()));
    user.MarkAsBot();

    var bot = new Bot
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Name = name,
        AvatarUrl = null,
        Strategy = strategy,
        Configuration = configuration,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    return (user, bot);
}
```

---

## Part 9: Frontend Updates

### 9.1 Updated Bot Types

**types/bot.ts (additions):**
```typescript
export type BotStrategy =
  | 'Random'
  | 'HomeFavorer'
  | 'UnderdogSupporter'
  | 'DrawPredictor'
  | 'HighScorer'
  | 'StatsAnalyst';  // New

export interface StatsAnalystConfig {
  formWeight: number;
  homeAdvantageWeight: number;
  goalTrendWeight: number;
  streakWeight: number;
  matchesAnalyzed: number;
  highStakesBoost: boolean;
  lateSeasonMatchday: number;
  style: 'Conservative' | 'Moderate' | 'Bold';
  randomVariance: number;
}

export interface Bot {
  id: string;
  name: string;
  avatarUrl: string | null;
  strategy: BotStrategy;
  configuration: string | null;  // JSON for StatsAnalyst config
  isActive: boolean;
  createdAt: string;
  lastBetPlacedAt: string | null;
}
```

### 9.2 Bot Strategy Descriptions

**components/bots/bot-strategy-info.tsx:**
```typescript
const strategyInfo: Record<BotStrategy, { icon: string; label: string; description: string }> = {
  Random: {
    icon: '🎲',
    label: 'Random',
    description: 'Makes random predictions'
  },
  HomeFavorer: {
    icon: '🏠',
    label: 'Home Favorer',
    description: 'Always backs the home team'
  },
  UnderdogSupporter: {
    icon: '🐕',
    label: 'Underdog',
    description: 'Loves an upset'
  },
  DrawPredictor: {
    icon: '🤝',
    label: 'Draw Expert',
    description: 'Expects stalemates'
  },
  HighScorer: {
    icon: '⚽',
    label: 'High Scorer',
    description: 'Predicts lots of goals'
  },
  StatsAnalyst: {
    icon: '🧠',
    label: 'Stats Analyst',
    description: 'Uses statistical analysis'
  }
};

export function BotStrategyInfo({ strategy }: { strategy: BotStrategy }) {
  const info = strategyInfo[strategy];
  return (
    <div className="flex items-center gap-2">
      <span>{info.icon}</span>
      <div>
        <p className="font-medium">{info.label}</p>
        <p className="text-sm text-muted-foreground">{info.description}</p>
      </div>
    </div>
  );
}
```

### 9.3 Bot Performance Comparison View

**components/bots/bot-performance.tsx:**
```typescript
// Shows how well each bot is performing in a league
interface BotPerformanceProps {
  leagueId: string;
}

export function BotPerformance({ leagueId }: BotPerformanceProps) {
  const { data: standings } = useLeagueStandings(leagueId);
  const { data: bots } = useLeagueBots(leagueId);

  // Filter standings to only bots
  const botStandings = standings?.filter(s =>
    bots?.some(b => b.id === s.userId)
  ) ?? [];

  return (
    <div className="space-y-4">
      <h3 className="font-semibold">Bot Performance</h3>
      {botStandings.map(standing => {
        const bot = bots?.find(b => b.id === standing.userId);
        return (
          <div key={standing.userId} className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <BotIndicator strategy={bot?.strategy ?? 'Random'} />
              <span>{bot?.name}</span>
            </div>
            <div className="text-right">
              <span className="font-bold">{standing.totalPoints} pts</span>
              <span className="text-sm text-muted-foreground ml-2">
                ({standing.exactMatches} exact)
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}
```

---

## Part 10: API Endpoints

### 10.1 Admin Endpoints for Stats Bots

**AdminBotsEndpoints.cs (additions):**
```csharp
// POST /api/admin/bots/stats-analyst - Create stats analyst bot
group.MapPost("/stats-analyst", CreateStatsAnalystBot)
    .WithName("CreateStatsAnalystBot");

// POST /api/admin/form-cache/refresh - Manually refresh form caches
group.MapPost("/form-cache/refresh", RefreshFormCaches)
    .WithName("RefreshFormCaches");

// GET /api/admin/form-cache/{teamId}/{competitionId} - Get cached form
group.MapGet("/form-cache/{teamId:guid}/{competitionId:guid}", GetTeamForm)
    .WithName("GetTeamForm");

private static async Task<IResult> CreateStatsAnalystBot(
    CreateStatsAnalystBotRequest request,
    IMediator mediator,
    CancellationToken cancellationToken)
{
    var config = new StatsAnalystConfig
    {
        FormWeight = request.FormWeight,
        HomeAdvantageWeight = request.HomeAdvantageWeight,
        GoalTrendWeight = request.GoalTrendWeight,
        StreakWeight = request.StreakWeight,
        MatchesAnalyzed = request.MatchesAnalyzed,
        HighStakesBoost = request.HighStakesBoost,
        Style = Enum.Parse<PredictionStyle>(request.Style),
        RandomVariance = request.RandomVariance
    };

    var command = new CreateBotCommand(
        request.Name,
        request.AvatarUrl,
        BotStrategy.StatsAnalyst,
        config.ToJson());

    var result = await mediator.Send(command, cancellationToken);
    return result.IsSuccess
        ? Results.Created($"/api/bots/{result.Value.Id}", result.Value)
        : Results.BadRequest(new { error = result.Error });
}

private static async Task<IResult> RefreshFormCaches(
    ITeamFormCalculator formCalculator,
    CancellationToken cancellationToken)
{
    await formCalculator.RefreshAllFormCachesAsync(cancellationToken);
    return Results.Ok(new { message = "Form caches refreshed" });
}
```

### 10.2 New Request DTOs

**CreateStatsAnalystBotRequest.cs:**
```csharp
public sealed record CreateStatsAnalystBotRequest(
    string Name,
    string? AvatarUrl,
    double FormWeight = 0.35,
    double HomeAdvantageWeight = 0.25,
    double GoalTrendWeight = 0.25,
    double StreakWeight = 0.15,
    int MatchesAnalyzed = 5,
    bool HighStakesBoost = true,
    string Style = "Moderate",
    double RandomVariance = 0.1);
```

---

## Part 11: Implementation Checklist

### Phase 7.5A: Domain Layer
- [ ] Add `StatsAnalyst` to `BotStrategy` enum
- [ ] Create `TeamFormCache` entity
- [ ] Create `StatsAnalystConfig` value object
- [ ] Create `PredictionStyle` enum
- [ ] Create `MatchAnalysis` record

### Phase 7.5B: Infrastructure Layer
- [ ] Create `TeamFormCacheConfiguration`
- [ ] Update `ApplicationDbContext` with `TeamFormCaches` DbSet
- [ ] Create database migration `AddTeamFormCache`
- [ ] Run migration

### Phase 7.5C: Application Layer - Form Calculator
- [ ] Create `ITeamFormCalculator` interface
- [ ] Implement `TeamFormCalculator` service
- [ ] Register in DI

### Phase 7.5D: Application Layer - Strategy
- [ ] Implement `StatsAnalystStrategy`
- [ ] Update `BotStrategyFactory` for DI
- [ ] Add configuration preset methods

### Phase 7.5E: Application Layer - Updated Services
- [ ] Update `BotBettingService` for async predictions
- [ ] Create `FormCacheBackgroundService`
- [ ] Register services in DI

### Phase 7.5F: API Layer
- [ ] Add admin endpoints for stats bots
- [ ] Add form cache refresh endpoint
- [ ] Create request DTOs

### Phase 7.5G: Seed Data
- [ ] Update `BotSeeder` with 6 new intelligent bots
- [ ] Verify bots are created on startup

### Phase 7.5H: Frontend Updates
- [ ] Update bot types with `StatsAnalyst`
- [ ] Create `BotStrategyInfo` component
- [ ] Create `BotPerformance` component
- [ ] Update bot indicator for stats bots

### Phase 7.5I: Testing & Verification
- [ ] Verify form calculation from match history
- [ ] Test all 6 configuration presets
- [ ] Verify predictions vary by configuration
- [ ] Test form cache refresh
- [ ] Compare bot performance in leaderboards

---

## Future Enhancements (Phase 9+)

When Phase 9 adds standings, H2H, scorers, and lineup data:

### Enhanced High Stakes Detection
```csharp
public bool IsHighStakesMatch(Match match, Standing homeStanding, Standing awayStanding)
{
    // Title race: both teams in top 4
    bool titleRace = homeStanding.Position <= 4 && awayStanding.Position <= 4;

    // Relegation battle: both teams in bottom 5
    bool relegationBattle = homeStanding.Position >= 16 && awayStanding.Position >= 16;

    // Top vs bottom clash
    bool topVsBottom = Math.Abs(homeStanding.Position - awayStanding.Position) >= 10;

    // Derby detection (same city, stored in Competition or external data)
    bool isDerby = IsDerbyMatch(match);

    return titleRace || relegationBattle || topVsBottom || isDerby;
}
```

### Head-to-Head Integration
```csharp
public (int HomeScore, int AwayScore) ApplyH2HModifier(
    int predictedHome,
    int predictedAway,
    HeadToHead h2h)
{
    if (h2h.HomeWins > h2h.AwayWins * 1.5)
    {
        // Home team historically dominates
        predictedHome += 1;
    }
    else if (h2h.AwayWins > h2h.HomeWins * 1.5)
    {
        // Away team historically dominates
        predictedAway += 1;
    }

    return (predictedHome, predictedAway);
}
```

---

## Lineup Analysis (Phase 9 Integration)

When Phase 9 provides lineup data, the StatsAnalyst strategy will analyze squad changes to detect weakened teams.

### New Configuration Options

Add to `StatsAnalystConfig`:
```csharp
// Lineup analysis weights (Phase 9)
public double LineupAnalysisWeight { get; init; } = 0.20;
public bool AnalyzeKeyPlayerAbsence { get; init; } = true;
public bool AnalyzeFormationChanges { get; init; } = true;
public bool AnalyzeGoalkeeperChange { get; init; } = true;
```

### Lineup Analysis Entities

**TeamUsualLineup.cs** - Tracks typical starting XI:
```csharp
public sealed class TeamUsualLineup : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    // Usual formation (e.g., "4-3-3", "4-4-2")
    public string UsualFormation { get; set; } = "";

    // Key players by position (JSON array of player IDs)
    public string UsualGoalkeeper { get; set; } = "";      // Player ID
    public string UsualDefenders { get; set; } = "";       // JSON: ["id1", "id2", "id3", "id4"]
    public string UsualMidfielders { get; set; } = "";     // JSON array
    public string UsualForwards { get; set; } = "";        // JSON array

    // Top scorer tracking
    public string TopScorerId { get; set; } = "";
    public string TopScorerName { get; set; } = "";
    public int TopScorerGoals { get; set; }

    // Captain/key player
    public string CaptainId { get; set; } = "";
    public string CaptainName { get; set; } = "";

    // Calculated from last N matches
    public int MatchesAnalyzed { get; set; }
    public DateTime CalculatedAt { get; set; }
}
```

**MatchLineupAnalysis.cs** - Analysis result for a specific match:
```csharp
public sealed record MatchLineupAnalysis
{
    public required Guid MatchId { get; init; }
    public required Guid TeamId { get; init; }

    // Lineup changes detected
    public bool GoalkeeperChanged { get; init; }
    public int DefendersChanged { get; init; }        // 0-4 typically
    public int MidfieldersChanged { get; init; }      // 0-4 typically
    public int ForwardsChanged { get; init; }         // 0-3 typically
    public bool FormationChanged { get; init; }
    public bool TopScorerAbsent { get; init; }
    public bool CaptainAbsent { get; init; }

    // Overall squad strength modifier (0.5 = very weak, 1.0 = normal, 1.1 = stronger)
    public double SquadStrengthModifier { get; init; }

    // Reason summary
    public string AnalysisSummary { get; init; } = "";
}
```

### Lineup Analyzer Service

**ILineupAnalyzer.cs**:
```csharp
public interface ILineupAnalyzer
{
    Task<TeamUsualLineup> CalculateUsualLineupAsync(
        Guid teamId,
        Guid competitionId,
        CancellationToken cancellationToken = default);

    Task<MatchLineupAnalysis> AnalyzeMatchLineupAsync(
        Guid matchId,
        Guid teamId,
        CancellationToken cancellationToken = default);

    double CalculateSquadStrengthModifier(MatchLineupAnalysis analysis);
}
```

**LineupAnalyzer.cs**:
```csharp
public sealed class LineupAnalyzer(
    IApplicationDbContext context,
    ILogger<LineupAnalyzer> logger) : ILineupAnalyzer
{
    public double CalculateSquadStrengthModifier(MatchLineupAnalysis analysis)
    {
        double modifier = 1.0;

        // Goalkeeper change is significant (first choice vs backup)
        if (analysis.GoalkeeperChanged)
        {
            modifier -= 0.08; // 8% reduction
            logger.LogDebug("Goalkeeper changed: -8%");
        }

        // Central defenders are crucial for stability
        if (analysis.DefendersChanged >= 2)
        {
            modifier -= 0.06 * analysis.DefendersChanged; // 6% per defender
            logger.LogDebug("{Count} defenders changed: -{Pct}%",
                analysis.DefendersChanged, analysis.DefendersChanged * 6);
        }

        // Midfield changes affect control
        if (analysis.MidfieldersChanged >= 2)
        {
            modifier -= 0.04 * analysis.MidfieldersChanged; // 4% per midfielder
        }

        // Forward changes affect goal threat
        if (analysis.ForwardsChanged >= 1)
        {
            modifier -= 0.05 * analysis.ForwardsChanged; // 5% per forward
        }

        // Top scorer absence is major
        if (analysis.TopScorerAbsent)
        {
            modifier -= 0.12; // 12% reduction
            logger.LogDebug("Top scorer absent: -12%");
        }

        // Captain absence affects leadership
        if (analysis.CaptainAbsent)
        {
            modifier -= 0.05; // 5% reduction
        }

        // Formation change suggests tactical uncertainty
        if (analysis.FormationChanged)
        {
            modifier -= 0.03; // 3% reduction
        }

        // Clamp between 0.5 (very weak) and 1.1 (slightly stronger than usual)
        return Math.Clamp(modifier, 0.5, 1.1);
    }

    public async Task<MatchLineupAnalysis> AnalyzeMatchLineupAsync(
        Guid matchId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var match = await context.Matches
            .Include(m => m.HomeLineup)
            .Include(m => m.AwayLineup)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        if (match == null)
            return CreateDefaultAnalysis(matchId, teamId);

        var usualLineup = await CalculateUsualLineupAsync(
            teamId, match.CompetitionId, cancellationToken);

        var currentLineup = match.HomeTeamId == teamId
            ? match.HomeLineup
            : match.AwayLineup;

        if (currentLineup == null)
            return CreateDefaultAnalysis(matchId, teamId);

        return CompareLineups(matchId, teamId, usualLineup, currentLineup);
    }

    private MatchLineupAnalysis CompareLineups(
        Guid matchId,
        Guid teamId,
        TeamUsualLineup usual,
        MatchLineup current)
    {
        var summaryParts = new List<string>();

        bool gkChanged = usual.UsualGoalkeeper != current.GoalkeeperId;
        if (gkChanged) summaryParts.Add("GK changed");

        var usualDefenders = JsonSerializer.Deserialize<List<string>>(usual.UsualDefenders) ?? [];
        int defendersChanged = current.DefenderIds.Count(d => !usualDefenders.Contains(d));
        if (defendersChanged > 0) summaryParts.Add($"{defendersChanged} DEF changed");

        var usualMidfielders = JsonSerializer.Deserialize<List<string>>(usual.UsualMidfielders) ?? [];
        int midfieldersChanged = current.MidfielderIds.Count(m => !usualMidfielders.Contains(m));
        if (midfieldersChanged > 0) summaryParts.Add($"{midfieldersChanged} MID changed");

        var usualForwards = JsonSerializer.Deserialize<List<string>>(usual.UsualForwards) ?? [];
        int forwardsChanged = current.ForwardIds.Count(f => !usualForwards.Contains(f));
        if (forwardsChanged > 0) summaryParts.Add($"{forwardsChanged} FWD changed");

        bool formationChanged = usual.UsualFormation != current.Formation;
        if (formationChanged) summaryParts.Add($"Formation: {usual.UsualFormation} → {current.Formation}");

        bool topScorerAbsent = !string.IsNullOrEmpty(usual.TopScorerId) &&
            !current.AllPlayerIds.Contains(usual.TopScorerId);
        if (topScorerAbsent) summaryParts.Add($"Top scorer {usual.TopScorerName} OUT");

        bool captainAbsent = !string.IsNullOrEmpty(usual.CaptainId) &&
            !current.AllPlayerIds.Contains(usual.CaptainId);
        if (captainAbsent) summaryParts.Add($"Captain {usual.CaptainName} OUT");

        var analysis = new MatchLineupAnalysis
        {
            MatchId = matchId,
            TeamId = teamId,
            GoalkeeperChanged = gkChanged,
            DefendersChanged = defendersChanged,
            MidfieldersChanged = midfieldersChanged,
            ForwardsChanged = forwardsChanged,
            FormationChanged = formationChanged,
            TopScorerAbsent = topScorerAbsent,
            CaptainAbsent = captainAbsent,
            AnalysisSummary = string.Join(", ", summaryParts)
        };

        // Calculate modifier using the analysis
        var modifier = CalculateSquadStrengthModifier(analysis);

        return analysis with { SquadStrengthModifier = modifier };
    }

    private MatchLineupAnalysis CreateDefaultAnalysis(Guid matchId, Guid teamId)
    {
        return new MatchLineupAnalysis
        {
            MatchId = matchId,
            TeamId = teamId,
            SquadStrengthModifier = 1.0,
            AnalysisSummary = "No lineup data available"
        };
    }
}
```

### Integration with StatsAnalystStrategy

Update the `AnalyzeMatch` method to include lineup analysis:

```csharp
private async Task<MatchAnalysis> AnalyzeMatchAsync(
    Match match,
    TeamFormCache homeForm,
    TeamFormCache awayForm,
    StatsAnalystConfig config,
    CancellationToken cancellationToken)
{
    // ... existing form analysis ...

    // Step 10: Apply lineup analysis (Phase 9)
    if (config.LineupAnalysisWeight > 0 && _lineupAnalyzer != null)
    {
        var homeLineupAnalysis = await _lineupAnalyzer.AnalyzeMatchLineupAsync(
            match.Id, match.HomeTeamId, cancellationToken);
        var awayLineupAnalysis = await _lineupAnalyzer.AnalyzeMatchLineupAsync(
            match.Id, match.AwayTeamId, cancellationToken);

        // Apply squad strength modifiers
        double homeLineupModifier = homeLineupAnalysis.SquadStrengthModifier;
        double awayLineupModifier = awayLineupAnalysis.SquadStrengthModifier;

        // Adjust expected goals based on squad strength
        expectedHomeGoals *= homeLineupModifier;
        expectedAwayGoals *= awayLineupModifier;

        // Log significant changes
        if (homeLineupModifier < 0.9)
        {
            logger.LogInformation(
                "Home team weakened ({Modifier:P0}): {Summary}",
                homeLineupModifier, homeLineupAnalysis.AnalysisSummary);
        }
        if (awayLineupModifier < 0.9)
        {
            logger.LogInformation(
                "Away team weakened ({Modifier:P0}): {Summary}",
                awayLineupModifier, awayLineupAnalysis.AnalysisSummary);
        }
    }

    // ... continue with prediction ...
}
```

### Impact Weights Summary

| Change | Impact | Rationale |
|--------|--------|-----------|
| Goalkeeper changed | -8% | Backup GK is typically much weaker |
| Each defender changed | -6% | Central defenders crucial for stability |
| Each midfielder changed | -4% | Affects ball control and tempo |
| Each forward changed | -5% | Impacts goal threat |
| Top scorer absent | -12% | Major goal threat missing |
| Captain absent | -5% | Leadership and experience loss |
| Formation changed | -3% | Tactical unfamiliarity |
| **Maximum penalty** | **-50%** | Heavily rotated squad |

### Example Scenarios

**Scenario 1: Full strength**
```
Team A: No changes
→ Modifier: 1.0 (100%)
→ Prediction: Based on normal form
```

**Scenario 2: Goalkeeper injury, backup plays**
```
Team A: GK changed
→ Modifier: 0.92 (92%)
→ Expected goals against increase by ~8%
```

**Scenario 3: Heavy rotation for cup match**
```
Team A: GK changed, 3 defenders, 2 midfielders, top scorer rested
→ Modifier: 0.92 - 0.18 - 0.08 - 0.12 = 0.54 (54%)
→ Significant underdog now
```

**Scenario 4: Top scorer red card suspension**
```
Team A: Top scorer absent
→ Modifier: 0.88 (88%)
→ Expected goals reduced by 12%
```

### New Bot Personality: Lineup Larry

Add a bot that heavily weights lineup analysis:

```csharp
public static StatsAnalystConfig LineupFocused => new()
{
    FormWeight = 0.20,
    HomeAdvantageWeight = 0.15,
    GoalTrendWeight = 0.15,
    StreakWeight = 0.10,
    LineupAnalysisWeight = 0.40,  // Heavy lineup focus
    AnalyzeKeyPlayerAbsence = true,
    AnalyzeFormationChanges = true,
    AnalyzeGoalkeeperChange = true,
    Style = PredictionStyle.Moderate
};
```

Bot seed:
```csharp
CreateBot("Lineup Larry", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.LineupFocused.ToJson(), "📋")
```

---

## Notes

### Performance Considerations
- Form cache refresh runs every 4 hours to balance freshness vs. database load
- Form is calculated from last 5 matches by default (configurable)
- Caches are unique per team-competition pair

### Configuration Flexibility
- All weights are 0.0-1.0 and should sum to approximately 1.0
- `RandomVariance` adds unpredictability (0.0 = deterministic, 0.3 = very random)
- `PredictionStyle` controls goal prediction aggressiveness

### Extensibility
- New factors can be added to `StatsAnalystConfig` without breaking existing bots
- Configuration JSON is forward-compatible (uses defaults for missing fields)
- Ready for standings/H2H integration in Phase 9
