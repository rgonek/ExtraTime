# Phase 9.5F: Graceful Degradation & StatsAnalyst Strategy

## Overview
Enable bots to detect which data sources are available before making predictions, redistribute weights when sources are missing, and fall back to simpler strategies when critical data is unavailable. Also includes the enhanced `StatsAnalystStrategy` with full external data source support.

> **Prerequisite**: Phases 9.5B-9.5E (data source integrations) should be in place before this phase

---

## Part 1: Prediction Context

### 1.1 PredictionContext

**File**: `src/ExtraTime.Application/Features/Bots/Strategies/PredictionContext.cs`

Captures what data is available for a prediction. Used to determine which factors can be used and how to weight them.

```csharp
namespace ExtraTime.Application.Features.Bots.Strategies;

/// <summary>
/// Context for a bot prediction, including data availability.
/// Used to determine which factors can be used and how to weight them.
/// </summary>
public sealed class PredictionContext
{
    public required Match Match { get; init; }
    public required StatsAnalystConfig Config { get; init; }
    public required DataAvailability DataAvailability { get; init; }

    // Collected data (null if unavailable)
    public TeamFormCache? HomeForm { get; set; }
    public TeamFormCache? AwayForm { get; set; }
    public TeamXgStats? HomeXg { get; set; }
    public TeamXgStats? AwayXg { get; set; }
    public MatchOdds? Odds { get; set; }
    public TeamInjuries? HomeInjuries { get; set; }
    public TeamInjuries? AwayInjuries { get; set; }
    public MatchLineupAnalysis? HomeLineupAnalysis { get; set; }
    public MatchLineupAnalysis? AwayLineupAnalysis { get; set; }
    public TeamEloRating? HomeElo { get; set; }
    public TeamEloRating? AwayElo { get; set; }

    // What's actually usable for this prediction
    public bool CanUseForm => HomeForm != null && AwayForm != null;
    public bool CanUseXg => DataAvailability.XgDataAvailable && HomeXg != null && AwayXg != null;
    public bool CanUseOdds => DataAvailability.OddsDataAvailable && Odds != null;
    public bool CanUseInjuries => DataAvailability.InjuryDataAvailable &&
                                   (HomeInjuries != null || AwayInjuries != null);
    public bool CanUseLineups => DataAvailability.LineupDataAvailable &&
                                  (HomeLineupAnalysis != null || AwayLineupAnalysis != null);
    public bool CanUseElo => HomeElo != null && AwayElo != null;

    // Calculate effective weights (redistribute unavailable weight)
    public EffectiveWeights CalculateEffectiveWeights()
    {
        var weights = new Dictionary<string, double>();
        double totalConfiguredWeight = 0;
        double totalAvailableWeight = 0;

        // Form is always available (calculated from matches)
        if (Config.FormWeight > 0)
        {
            weights["Form"] = Config.FormWeight;
            totalConfiguredWeight += Config.FormWeight;
            if (CanUseForm) totalAvailableWeight += Config.FormWeight;
        }

        if (Config.HomeAdvantageWeight > 0)
        {
            weights["HomeAdvantage"] = Config.HomeAdvantageWeight;
            totalConfiguredWeight += Config.HomeAdvantageWeight;
            totalAvailableWeight += Config.HomeAdvantageWeight; // Always available
        }

        if (Config.XgWeight > 0)
        {
            weights["Xg"] = Config.XgWeight;
            totalConfiguredWeight += Config.XgWeight;
            if (CanUseXg) totalAvailableWeight += Config.XgWeight;
        }

        if (Config.XgDefensiveWeight > 0)
        {
            weights["XgDefensive"] = Config.XgDefensiveWeight;
            totalConfiguredWeight += Config.XgDefensiveWeight;
            if (CanUseXg) totalAvailableWeight += Config.XgDefensiveWeight;
        }

        if (Config.OddsWeight > 0)
        {
            weights["Odds"] = Config.OddsWeight;
            totalConfiguredWeight += Config.OddsWeight;
            if (CanUseOdds) totalAvailableWeight += Config.OddsWeight;
        }

        if (Config.InjuryWeight > 0)
        {
            weights["Injury"] = Config.InjuryWeight;
            totalConfiguredWeight += Config.InjuryWeight;
            if (CanUseInjuries) totalAvailableWeight += Config.InjuryWeight;
        }

        if (Config.LineupAnalysisWeight > 0)
        {
            weights["Lineup"] = Config.LineupAnalysisWeight;
            totalConfiguredWeight += Config.LineupAnalysisWeight;
            if (CanUseLineups) totalAvailableWeight += Config.LineupAnalysisWeight;
        }

        if (Config.EloWeight > 0)
        {
            weights["Elo"] = Config.EloWeight;
            totalConfiguredWeight += Config.EloWeight;
            if (CanUseElo) totalAvailableWeight += Config.EloWeight;
        }

        // Redistribute: scale available weights up to compensate for missing ones
        double scaleFactor = totalAvailableWeight > 0
            ? totalConfiguredWeight / totalAvailableWeight
            : 1.0;

        return new EffectiveWeights
        {
            FormWeight = CanUseForm ? Config.FormWeight * scaleFactor : 0,
            HomeAdvantageWeight = Config.HomeAdvantageWeight * scaleFactor,
            XgWeight = CanUseXg ? Config.XgWeight * scaleFactor : 0,
            XgDefensiveWeight = CanUseXg ? Config.XgDefensiveWeight * scaleFactor : 0,
            OddsWeight = CanUseOdds ? Config.OddsWeight * scaleFactor : 0,
            InjuryWeight = CanUseInjuries ? Config.InjuryWeight * scaleFactor : 0,
            LineupWeight = CanUseLineups ? Config.LineupAnalysisWeight * scaleFactor : 0,
            EloWeight = CanUseElo ? Config.EloWeight * scaleFactor : 0,
            TotalConfiguredSources = weights.Count,
            TotalAvailableSources = new[] { CanUseForm, true, CanUseXg, CanUseOdds, CanUseInjuries, CanUseLineups, CanUseElo }.Count(x => x),
            DataQualityScore = totalAvailableWeight / totalConfiguredWeight * 100
        };
    }

    // Determine if this bot can make a meaningful prediction
    public bool CanMakePrediction()
    {
        // Always need basic form data
        if (!CanUseForm) return false;

        // Check if bot is heavily dependent on unavailable data
        var effectiveWeights = CalculateEffectiveWeights();

        // If we've lost more than 50% of configured weight, prediction quality is low
        if (effectiveWeights.DataQualityScore < 50) return false;

        return true;
    }

    // Get warning message for degraded predictions
    public string? GetDegradationWarning()
    {
        var missing = new List<string>();

        if (Config.XgWeight > 0.15 && !CanUseXg)
            missing.Add("xG data unavailable");
        if (Config.OddsWeight > 0.15 && !CanUseOdds)
            missing.Add("Odds data unavailable");
        if (Config.InjuryWeight > 0.10 && !CanUseInjuries)
            missing.Add("Injury data unavailable");
        if (Config.LineupAnalysisWeight > 0.10 && !CanUseLineups)
            missing.Add("Lineup data unavailable");
        if (Config.EloWeight > 0.10 && !CanUseElo)
            missing.Add("Elo rating data unavailable");

        return missing.Count > 0
            ? $"Degraded prediction: {string.Join(", ", missing)}"
            : null;
    }
}

public sealed record EffectiveWeights
{
    public double FormWeight { get; init; }
    public double HomeAdvantageWeight { get; init; }
    public double XgWeight { get; init; }
    public double XgDefensiveWeight { get; init; }
    public double OddsWeight { get; init; }
    public double InjuryWeight { get; init; }
    public double LineupWeight { get; init; }
    public double EloWeight { get; init; }

    public int TotalConfiguredSources { get; init; }
    public int TotalAvailableSources { get; init; }
    public double DataQualityScore { get; init; }  // 0-100
}
```

### 1.2 FallbackStrategy

**File**: `src/ExtraTime.Application/Features/Bots/Strategies/FallbackStrategy.cs`

```csharp
public sealed class FallbackStrategy
{
    private readonly Random _random = new();

    /// <summary>
    /// Simple prediction based only on home advantage.
    /// Used when data quality is too low for sophisticated analysis.
    /// </summary>
    public (int HomeScore, int AwayScore) GenerateBasicPrediction()
    {
        // Simple: home team slight advantage
        int homeScore = _random.NextDouble() < 0.55 ? 2 : 1;
        int awayScore = _random.NextDouble() < 0.40 ? 1 : 0;

        // Occasionally predict draws (20%)
        if (_random.NextDouble() < 0.20)
        {
            awayScore = homeScore;
        }

        return (homeScore, awayScore);
    }
}
```

---

## Part 2: Updated StatsAnalystConfig

### 2.1 Extended Configuration

**File**: `src/ExtraTime.Application/Features/Bots/Strategies/StatsAnalystConfig.cs` (extended)

```csharp
public sealed record StatsAnalystConfig
{
    // === Existing weights (Phase 7.5) ===
    public double FormWeight { get; init; } = 0.20;
    public double HomeAdvantageWeight { get; init; } = 0.15;
    public double GoalTrendWeight { get; init; } = 0.10;
    public double StreakWeight { get; init; } = 0.05;

    // === Phase 9 weights ===
    public double LineupAnalysisWeight { get; init; } = 0.10;

    // === Phase 9.5 weights - External Data ===
    public double XgWeight { get; init; } = 0.20;           // Understat xG data
    public double XgDefensiveWeight { get; init; } = 0.10;  // xGA data
    public double OddsWeight { get; init; } = 0.05;         // Market consensus
    public double InjuryWeight { get; init; } = 0.05;       // Injury impact
    public double EloWeight { get; init; } = 0.00;          // Elo rating weight

    // === Analysis parameters ===
    public int MatchesAnalyzed { get; init; } = 5;
    public bool HighStakesBoost { get; init; } = true;
    public int LateSeasonMatchday { get; init; } = 30;

    // === Prediction style ===
    public PredictionStyle Style { get; init; } = PredictionStyle.Moderate;
    public double RandomVariance { get; init; } = 0.1;

    // === Feature flags ===
    public bool UseXgData { get; init; } = true;
    public bool UseOddsData { get; init; } = true;
    public bool UseInjuryData { get; init; } = true;
    public bool UseLineupData { get; init; } = true;
    public bool UseEloData { get; init; } = true;

    // === Preset configurations ===

    public static StatsAnalystConfig FullAnalysis => new()
    {
        FormWeight = 0.15,
        HomeAdvantageWeight = 0.10,
        GoalTrendWeight = 0.05,
        StreakWeight = 0.05,
        LineupAnalysisWeight = 0.10,
        XgWeight = 0.25,
        XgDefensiveWeight = 0.15,
        OddsWeight = 0.10,
        InjuryWeight = 0.05,
        Style = PredictionStyle.Moderate
    };

    public static StatsAnalystConfig XgFocused => new()
    {
        FormWeight = 0.10,
        HomeAdvantageWeight = 0.10,
        XgWeight = 0.40,
        XgDefensiveWeight = 0.25,
        OddsWeight = 0.10,
        InjuryWeight = 0.05,
        Style = PredictionStyle.Moderate
    };

    public static StatsAnalystConfig MarketFollower => new()
    {
        FormWeight = 0.15,
        HomeAdvantageWeight = 0.10,
        XgWeight = 0.15,
        OddsWeight = 0.50,  // Heavy market weight
        InjuryWeight = 0.10,
        Style = PredictionStyle.Conservative
    };

    public static StatsAnalystConfig InjuryAware => new()
    {
        FormWeight = 0.20,
        HomeAdvantageWeight = 0.15,
        XgWeight = 0.20,
        LineupAnalysisWeight = 0.20,
        InjuryWeight = 0.25,  // Heavy injury weight
        Style = PredictionStyle.Moderate
    };
}
```

---

## Part 3: Enhanced StatsAnalystStrategy

### 3.1 Updated Strategy Implementation

**File**: `src/ExtraTime.Application/Features/Bots/Strategies/StatsAnalystStrategy.cs` (Phase 9.5 updates)

```csharp
public sealed class StatsAnalystStrategy : IBotBettingStrategy
{
    private readonly ITeamFormCalculator _formCalculator;
    private readonly IUnderstatService? _understatService;
    private readonly IOddsDataService? _oddsService;
    private readonly IInjuryService? _injuryService;
    private readonly ILineupAnalyzer? _lineupAnalyzer;
    private readonly IEloRatingService? _eloService;
    private readonly IApplicationDbContext _context;
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.StatsAnalyst;

    public StatsAnalystStrategy(
        ITeamFormCalculator formCalculator,
        IApplicationDbContext context,
        IUnderstatService? understatService = null,
        IOddsDataService? oddsService = null,
        IInjuryService? injuryService = null,
        ILineupAnalyzer? lineupAnalyzer = null,
        IEloRatingService? eloService = null)
    {
        _formCalculator = formCalculator;
        _context = context;
        _understatService = understatService;
        _oddsService = oddsService;
        _injuryService = injuryService;
        _lineupAnalyzer = lineupAnalyzer;
        _eloService = eloService;
    }

    public async Task<(int HomeScore, int AwayScore)> GeneratePredictionAsync(
        Match match,
        string? configuration,
        CancellationToken cancellationToken = default)
    {
        var config = StatsAnalystConfig.FromJson(configuration);

        // Gather all available data
        var analysisData = await GatherAnalysisDataAsync(match, config, cancellationToken);

        // Calculate expected goals for each team
        var (expectedHome, expectedAway) = CalculateExpectedGoals(analysisData, config);

        // Apply style and variance
        var (homeScore, awayScore) = ConvertToScoreline(expectedHome, expectedAway, config);

        return (homeScore, awayScore);
    }

    private async Task<MatchAnalysisData> GatherAnalysisDataAsync(
        Match match,
        StatsAnalystConfig config,
        CancellationToken cancellationToken)
    {
        var data = new MatchAnalysisData { Match = match };

        // Form data (always available from Phase 7.5)
        data.HomeForm = await _formCalculator.CalculateFormAsync(
            match.HomeTeamId, match.CompetitionId, config.MatchesAnalyzed, cancellationToken);
        data.AwayForm = await _formCalculator.CalculateFormAsync(
            match.AwayTeamId, match.CompetitionId, config.MatchesAnalyzed, cancellationToken);

        // xG data (Phase 9.5 - Understat)
        if (config.UseXgData && _understatService != null)
        {
            data.HomeXg = await GetTeamXgAsync(match.HomeTeamId, match.CompetitionId, cancellationToken);
            data.AwayXg = await GetTeamXgAsync(match.AwayTeamId, match.CompetitionId, cancellationToken);
        }

        // Odds data (Phase 9.5 - Football-Data.co.uk)
        if (config.UseOddsData && _oddsService != null)
        {
            data.Odds = await _oddsService.GetOddsForMatchAsync(match.Id, cancellationToken);
        }

        // Injury data (Phase 9.5 - API-Football)
        if (config.UseInjuryData && _injuryService != null)
        {
            data.HomeInjuries = await _injuryService.GetTeamInjuriesAsync(match.HomeTeamId, cancellationToken);
            data.AwayInjuries = await _injuryService.GetTeamInjuriesAsync(match.AwayTeamId, cancellationToken);
        }

        // Lineup analysis (Phase 9)
        if (config.UseLineupData && _lineupAnalyzer != null)
        {
            data.HomeLineupAnalysis = await _lineupAnalyzer.AnalyzeMatchLineupAsync(
                match.Id, match.HomeTeamId, cancellationToken);
            data.AwayLineupAnalysis = await _lineupAnalyzer.AnalyzeMatchLineupAsync(
                match.Id, match.AwayTeamId, cancellationToken);
        }

        // Elo data (Phase 9.5D - ClubElo)
        if (config.UseEloData && _eloService != null)
        {
            data.HomeElo = await _eloService.GetTeamEloAsync(match.HomeTeamId, cancellationToken);
            data.AwayElo = await _eloService.GetTeamEloAsync(match.AwayTeamId, cancellationToken);
        }

        return data;
    }

    private (double homeExpected, double awayExpected) CalculateExpectedGoals(
        MatchAnalysisData data,
        StatsAnalystConfig config)
    {
        double homeBase = 1.5; // League average
        double awayBase = 1.2; // Slight away disadvantage

        // === Form-based adjustments ===
        if (config.FormWeight > 0 && data.HomeForm != null && data.AwayForm != null)
        {
            var homeFormModifier = data.HomeForm.GetFormScore() / 50.0; // Normalize to ~1.0
            var awayFormModifier = data.AwayForm.GetFormScore() / 50.0;

            homeBase *= (1 + (homeFormModifier - 1) * config.FormWeight);
            awayBase *= (1 + (awayFormModifier - 1) * config.FormWeight);
        }

        // === xG-based adjustments (Phase 9.5) ===
        if (config.XgWeight > 0 && data.HomeXg != null && data.AwayXg != null)
        {
            // Use xG per match as base
            var homeXgBase = data.HomeXg.XgPerMatch;
            var awayXgBase = data.AwayXg.XgPerMatch;

            // Weight by xG data
            homeBase = homeBase * (1 - config.XgWeight) + homeXgBase * config.XgWeight;
            awayBase = awayBase * (1 - config.XgWeight) + awayXgBase * config.XgWeight;

            // Defensive xG adjustment
            if (config.XgDefensiveWeight > 0)
            {
                // If opponent concedes high xGA, increase expected goals
                homeBase *= 1 + (data.AwayXg.XgAgainstPerMatch - 1.3) * config.XgDefensiveWeight;
                awayBase *= 1 + (data.HomeXg.XgAgainstPerMatch - 1.3) * config.XgDefensiveWeight;
            }
        }

        // === Home advantage adjustment ===
        if (config.HomeAdvantageWeight > 0)
        {
            homeBase *= 1 + (0.15 * config.HomeAdvantageWeight); // ~15% home boost
            awayBase *= 1 - (0.10 * config.HomeAdvantageWeight); // ~10% away penalty
        }

        // === Odds-based adjustment (Phase 9.5) ===
        if (config.OddsWeight > 0 && data.Odds != null)
        {
            // If market heavily favors home win, boost home expected goals
            if (data.Odds.MarketFavorite == MatchOutcome.HomeWin)
            {
                var confidence = data.Odds.FavoriteConfidence;
                homeBase *= 1 + ((confidence - 0.4) * config.OddsWeight);
            }
            else if (data.Odds.MarketFavorite == MatchOutcome.AwayWin)
            {
                var confidence = data.Odds.FavoriteConfidence;
                awayBase *= 1 + ((confidence - 0.3) * config.OddsWeight);
            }
        }

        // === Injury adjustment (Phase 9.5) ===
        if (config.InjuryWeight > 0)
        {
            if (data.HomeInjuries != null)
            {
                var impact = data.HomeInjuries.InjuryImpactScore / 100.0;
                homeBase *= 1 - (impact * config.InjuryWeight);
            }
            if (data.AwayInjuries != null)
            {
                var impact = data.AwayInjuries.InjuryImpactScore / 100.0;
                awayBase *= 1 - (impact * config.InjuryWeight);
            }
        }

        // === Lineup adjustment (Phase 9) ===
        if (config.LineupAnalysisWeight > 0)
        {
            if (data.HomeLineupAnalysis != null)
            {
                homeBase *= data.HomeLineupAnalysis.SquadStrengthModifier;
            }
            if (data.AwayLineupAnalysis != null)
            {
                awayBase *= data.AwayLineupAnalysis.SquadStrengthModifier;
            }
        }

        // === Elo adjustment (Phase 9.5D) ===
        if (config.EloWeight > 0 && data.HomeElo != null && data.AwayElo != null)
        {
            // Elo difference drives expected goals
            var eloDiff = data.HomeElo.EloRating - data.AwayElo.EloRating;
            var eloModifier = eloDiff / 400.0; // Normalize: 400 Elo diff ~= 1 goal

            homeBase *= 1 + (eloModifier * config.EloWeight);
            awayBase *= 1 - (eloModifier * config.EloWeight);
        }

        return (homeBase, awayBase);
    }

    private (int home, int away) ConvertToScoreline(
        double homeExpected,
        double awayExpected,
        StatsAnalystConfig config)
    {
        // Apply random variance
        if (config.RandomVariance > 0)
        {
            homeExpected += (_random.NextDouble() - 0.5) * 2 * config.RandomVariance * homeExpected;
            awayExpected += (_random.NextDouble() - 0.5) * 2 * config.RandomVariance * awayExpected;
        }

        // Convert to goals based on style
        int homeGoals = config.Style switch
        {
            PredictionStyle.Conservative => (int)Math.Floor(homeExpected),
            PredictionStyle.Bold => (int)Math.Ceiling(homeExpected),
            _ => (int)Math.Round(homeExpected)
        };

        int awayGoals = config.Style switch
        {
            PredictionStyle.Conservative => (int)Math.Floor(awayExpected),
            PredictionStyle.Bold => (int)Math.Ceiling(awayExpected),
            _ => (int)Math.Round(awayExpected)
        };

        // Clamp to valid range
        homeGoals = Math.Clamp(homeGoals, 0, 6);
        awayGoals = Math.Clamp(awayGoals, 0, 5);

        return (homeGoals, awayGoals);
    }

    private async Task<TeamXgStats?> GetTeamXgAsync(
        Guid teamId,
        Guid competitionId,
        CancellationToken cancellationToken)
    {
        var season = DateTime.UtcNow.Year.ToString();
        if (DateTime.UtcNow.Month < 8) season = (DateTime.UtcNow.Year - 1).ToString();

        return await _context.TeamXgStats
            .FirstOrDefaultAsync(x =>
                x.TeamId == teamId &&
                x.CompetitionId == competitionId &&
                x.Season == season,
                cancellationToken);
    }

    // Interface compliance
    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        // Sync wrapper - uses cached data only
        return (1, 1); // Fallback
    }
}

internal sealed class MatchAnalysisData
{
    public required Match Match { get; init; }
    public TeamFormCache? HomeForm { get; set; }
    public TeamFormCache? AwayForm { get; set; }
    public TeamXgStats? HomeXg { get; set; }
    public TeamXgStats? AwayXg { get; set; }
    public MatchOdds? Odds { get; set; }
    public TeamInjuries? HomeInjuries { get; set; }
    public TeamInjuries? AwayInjuries { get; set; }
    public MatchLineupAnalysis? HomeLineupAnalysis { get; set; }
    public MatchLineupAnalysis? AwayLineupAnalysis { get; set; }
    public TeamEloRating? HomeElo { get; set; }
    public TeamEloRating? AwayElo { get; set; }
}
```

---

## Implementation Checklist

### Phase 9.5F: Graceful Degradation
- [ ] Create `PredictionContext` class (with `CanUseElo` check)
- [ ] Create `EffectiveWeights` record (with `EloWeight`)
- [ ] Implement weight redistribution logic
- [ ] Create `FallbackStrategy` class
- [ ] Update `StatsAnalystStrategy` to check data availability
- [ ] Add degradation warnings to predictions
- [ ] Test bot behavior with missing data sources

### Phase 9.5D: Enhanced StatsAnalyst Strategy
- [ ] Update `StatsAnalystConfig` with new weights (`EloWeight`)
- [ ] Add preset configurations
- [ ] Update `StatsAnalystStrategy` with Elo + all external data sources
- [ ] Update `BotStrategyFactory` with new dependencies
- [ ] Test prediction accuracy

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Application/Features/Bots/Strategies/PredictionContext.cs` |
| **Create** | `Application/Features/Bots/Strategies/FallbackStrategy.cs` |
| **Modify** | `Application/Features/Bots/Strategies/StatsAnalystConfig.cs` (add EloWeight, UseEloData, presets) |
| **Modify** | `Application/Features/Bots/Strategies/StatsAnalystStrategy.cs` (add Elo, full data gathering) |
| **Modify** | `Application/Features/Bots/Strategies/MatchAnalysisData.cs` (add Elo fields) |
