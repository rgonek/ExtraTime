# Phase 7.8: ML.NET Bot Integration - Detailed Implementation Plan

## Overview
Implement machine learning-based betting bots using ML.NET with multi-output regression to predict exact match scores (0-5). Models trained monthly with local trainer console app, predictions served via Azure Function. Includes comprehensive performance tracking and admin dashboard.

**Goal**: Create ML-powered bots that learn from historical match data to predict exact scores more accurately than rule-based strategies.

**Prerequisites**: Phase 7 (Basic Bots) and Phase 7.5 (StatsAnalyst) complete

## Implementation Progress

- [x] Part 1: Domain Layer - New Entities
- [x] Part 2: Application Layer - ML Feature Extraction
- [x] Part 3: Trainer Console Application
- [x] Part 4: Prediction Service
- [x] Part 5: Bot Strategy Integration
- [ ] Part 6: Azure Function for Predictions
- [ ] Part 7: Admin Dashboard
- [ ] Part 8: Frontend Components
- [ ] Part 9: Integration & Testing

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                    ML.NET Model Architecture                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐          │
│  │   Training   │────▶│ Azure Blob   │────▶│  Prediction  │          │
│  │   (Monthly)  │     │   Storage    │     │  (Real-time) │          │
│  └──────────────┘     └──────────────┘     └──────────────┘          │
│         │                                           │                 │
│         │ Local OR Azure Function                   │                 │
│         ▼                                           ▼                 │
│  ┌──────────────┐                          ┌──────────────┐          │
│  │   Trainer    │                          │  MLService   │          │
│  │   Console    │                          │   (Azure)    │          │
│  │   (Dev box)  │                          │  Function    │          │
│  └──────────────┘                          └──────────────┘          │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                    Azure SQL Database                            │ │
│  │  - Matches (historical results for training)                     │ │
│  │  - MlModelVersion (model metadata & metrics)                     │ │
│  │  - BotPredictionAccuracy (performance tracking)                  │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Part 1: Domain Layer - New Entities

### 1.1 MlModelVersion Entity

Track trained model versions with performance metrics.

**File**: `src/ExtraTime.Domain/Entities/MlModelVersion.cs`

```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// Tracks trained ML.NET model versions with performance metrics.
/// Binary model data stored in Azure Blob, metadata here.
/// </summary>
public sealed class MlModelVersion : BaseEntity
{
    public string ModelType { get; set; } = null!;  // "HomeScore" or "AwayScore"
    public string Version { get; set; } = null!;    // "v2024-01-15-001"
    public string? BlobPath { get; set; }            // Path in Azure Blob Storage
    
    // Training metadata
    public DateTime TrainedAt { get; set; }
    public int TrainingSamples { get; set; }
    public string? TrainingDataRange { get; set; }   // "2023-08 to 2024-01"
    
    // Performance metrics
    public double Rsquared { get; set; }             // R-squared (coefficient of determination)
    public double MeanAbsoluteError { get; set; }    // MAE in goals
    public double RootMeanSquaredError { get; set; } // RMSE
    public double MeanAbsolutePercentageError { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? ActivationNotes { get; set; }
    
    // Feature importance (JSON)
    public string? FeatureImportanceJson { get; set; }
    
    // Training configuration
    public string? AlgorithmUsed { get; set; }       // "Sdca", "FastTree", etc.
    public string? HyperparametersJson { get; set; } // Training hyperparameters
    
    // Cross-validation results
    public double CrossValidationMAE { get; set; }
    public double CrossValidationStdDev { get; set; }
}
```

### 1.2 BotPredictionAccuracy Entity

Track prediction accuracy per bot strategy for comparison.

**File**: `src/ExtraTime.Domain/Entities/BotPredictionAccuracy.cs`

```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// Tracks prediction accuracy for each bot strategy.
/// Updated after each match result is known.
/// </summary>
public sealed class BotPredictionAccuracy : BaseEntity
{
    public Guid BotId { get; set; }
    public Bot Bot { get; set; } = null!;
    
    public BotStrategy Strategy { get; set; }
    public string? ModelVersion { get; set; }        // For ML bots: "v2024-01-15-001"
    
    // Time window
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodType { get; set; } = "weekly"; // "weekly", "monthly", "all-time"
    
    // Prediction counts
    public int TotalPredictions { get; set; }
    public int ExactScores { get; set; }             // Perfect score prediction
    public int CorrectResults { get; set; }          // Win/Draw/Loss correct
    public int GoalsOffBy1 { get; set; }             // Total goals off by 1
    public int GoalsOffBy2 { get; set; }             // Total goals off by 2
    public int GoalsOffBy3Plus { get; set; }
    
    // Accuracy metrics
    public double ExactScoreAccuracy { get; set; }   // % of exact scores
    public double CorrectResultAccuracy { get; set; } // % correct results
    public double Within1GoalAccuracy { get; set; }  // % within 1 goal
    
    // Error metrics
    public double MeanAbsoluteError { get; set; }    // Avg goal difference
    public double RootMeanSquaredError { get; set; }
    public double HomeScoreMAE { get; set; }
    public double AwayScoreMAE { get; set; }
    
    // Betting performance
    public double TotalPointsEarned { get; set; }
    public double AvgPointsPerBet { get; set; }
    public int BetsWon { get; set; }
    public int BetsLost { get; set; }
    
    // Metadata
    public DateTime LastUpdatedAt { get; set; }
    public string? CalculationNotes { get; set; }
}
```

### 1.3 Update Match Entity

Add computed properties for ML training targets.

**File**: `src/ExtraTime.Domain/Entities/Match.cs`

Add to existing Match entity:

```csharp
public partial class Match
{
    // Add these computed properties for ML training
    
    /// <summary>
    /// True if match is completed and has valid score for training
    /// </summary>
    public bool IsValidForTraining => 
        Status == MatchStatus.Finished && 
        HomeScore.HasValue && 
        AwayScore.HasValue &&
        HomeScore.Value >= 0 && 
        AwayScore.Value >= 0;
    
    /// <summary>
    /// Returns match outcome as int: 0=HomeWin, 1=Draw, 2=AwayWin
    /// Used for classification models
    /// </summary>
    public int? MatchOutcome => 
        !IsValidForTraining ? null :
        HomeScore > AwayScore ? 0 :
        HomeScore == AwayScore ? 1 :
        2;
}
```

### 1.4 Add Entity Configurations

**File**: `src/ExtraTime.Infrastructure/Data/Configurations/MlModelVersionConfiguration.cs`

```csharp
namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class MlModelVersionConfiguration : IEntityTypeConfiguration<MlModelVersion>
{
    public void Configure(EntityTypeBuilder<MlModelVersion> builder)
    {
        builder.ToTable("MlModelVersions");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.ModelType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(m => m.Version)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(m => m.BlobPath)
            .HasMaxLength(500);
            
        builder.Property(m => m.TrainingDataRange)
            .HasMaxLength(100);
            
        builder.Property(m => m.AlgorithmUsed)
            .HasMaxLength(100);
            
        builder.Property(m => m.ActivationNotes)
            .HasMaxLength(500);
            
        builder.HasIndex(m => new { m.ModelType, m.IsActive })
            .HasDatabaseName("IX_MlModelVersions_Type_Active");
            
        builder.HasIndex(m => m.Version)
            .IsUnique()
            .HasDatabaseName("IX_MlModelVersions_Version");
    }
}
```

**File**: `src/ExtraTime.Infrastructure/Data/Configurations/BotPredictionAccuracyConfiguration.cs`

```csharp
namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class BotPredictionAccuracyConfiguration : IEntityTypeConfiguration<BotPredictionAccuracy>
{
    public void Configure(EntityTypeBuilder<BotPredictionAccuracy> builder)
    {
        builder.ToTable("BotPredictionAccuracies");
        
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Strategy)
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(b => b.ModelVersion)
            .HasMaxLength(50);
            
        builder.Property(b => b.PeriodType)
            .HasMaxLength(20);
            
        builder.Property(b => b.CalculationNotes)
            .HasMaxLength(1000);
            
        builder.HasOne(b => b.Bot)
            .WithMany()
            .HasForeignKey(b => b.BotId)
            .HasConstraintName("FK_BotPredictionAccuracies_Bots_BotId");
            
        builder.HasIndex(b => new { b.BotId, b.PeriodType, b.PeriodStart })
            .HasDatabaseName("IX_BotPredictionAccuracies_Bot_Period");
    }
}
```

---

## Part 2: Application Layer - ML Feature Extraction

### 2.1 MatchFeatures Data Class

**File**: `src/ExtraTime.Application/Features/ML/Models/MatchFeatures.cs`

```csharp
namespace ExtraTime.Application.Features.ML.Models;

/// <summary>
/// Feature vector for ML.NET training and prediction.
/// All features must be numeric (float) for ML.NET.
/// </summary>
public sealed class MatchFeatures
{
    // Match identifiers (not used for training, but useful for tracking)
    public string MatchId { get; set; } = null!;
    public string HomeTeamId { get; set; } = null!;
    public string AwayTeamId { get; set; } = null!;
    
    // === TEAM FORM FEATURES (Home Team) ===
    
    // Last 5 matches form
    public float HomeFormPointsLast5 { get; set; }      // Points from last 5: 0-15
    public float HomeGoalsScoredLast5 { get; set; }     // Goals scored in last 5
    public float HomeGoalsConcededLast5 { get; set; }   // Goals conceded in last 5
    public float HomeCleanSheetsLast5 { get; set; }     // Count: 0-5
    public float HomeWinsLast5 { get; set; }            // Count: 0-5
    public float HomeDrawsLast5 { get; set; }           // Count: 0-5
    public float HomeLossesLast5 { get; set; }          // Count: 0-5
    
    // Season averages
    public float HomeGoalsScoredAvg { get; set; }       // Goals per match
    public float HomeGoalsConcededAvg { get; set; }     // Goals per match
    public float HomePointsPerGame { get; set; }        // Season average
    
    // Home/away specific
    public float HomeGoalsScoredAtHomeAvg { get; set; }
    public float HomeGoalsConcededAtHomeAvg { get; set; }
    public float HomeWinRateAtHome { get; set; }        // 0.0-1.0
    
    // === TEAM FORM FEATURES (Away Team) ===
    
    public float AwayFormPointsLast5 { get; set; }
    public float AwayGoalsScoredLast5 { get; set; }
    public float AwayGoalsConcededLast5 { get; set; }
    public float AwayCleanSheetsLast5 { get; set; }
    public float AwayWinsLast5 { get; set; }
    public float AwayDrawsLast5 { get; set; }
    public float AwayLossesLast5 { get; set; }
    
    public float AwayGoalsScoredAvg { get; set; }
    public float AwayGoalsConcededAvg { get; set; }
    public float AwayPointsPerGame { get; set; }
    
    public float AwayGoalsScoredAwayAvg { get; set; }
    public float AwayGoalsConcededAwayAvg { get; set; }
    public float AwayWinRateAway { get; set; }
    
    // === HEAD-TO-HEAD FEATURES ===
    
    public float H2HMatchesPlayed { get; set; }         // Total H2H matches
    public float H2HHomeWins { get; set; }              // Home team wins
    public float H2HAwayWins { get; set; }              // Away team wins
    public float H2HDraws { get; set; }
    public float H2HHomeGoalsAvg { get; set; }          // Avg goals in H2H
    public float H2HAwayGoalsAvg { get; set; }
    public float H2HBttsRate { get; set; }              // Both teams scored rate
    public float H2HOver2_5Rate { get; set; }           // Over 2.5 goals rate
    
    // Recent H2H (last 3 meetings)
    public float H2HRecentHomeWins { get; set; }
    public float H2HRecentAwayWins { get; set; }
    
    // === LEAGUE CONTEXT FEATURES ===
    
    public float LeagueAvgHomeGoals { get; set; }       // League-wide average
    public float LeagueAvgAwayGoals { get; set; }
    public float LeagueHomeAdvantage { get; set; }      // Home team advantage factor
    public float SeasonProgress { get; set; }           // % of season complete: 0.0-1.0
    
    // === TEAM QUALITY INDICATORS ===
    
    public float HomeLeaguePosition { get; set; }       // 1-20
    public float AwayLeaguePosition { get; set; }
    public float PositionDifference { get; set; }       // Home - Away
    public float HomeIsTopHalf { get; set; }            // 1.0 if top 10, 0.0 otherwise
    public float AwayIsTopHalf { get; set; }
    
    // === ODDS FEATURES (if available) ===
    
    public float HomeOdds { get; set; }                 // Bookmaker odds
    public float DrawOdds { get; set; }
    public float AwayOdds { get; set; }
    public float ImpliedHomeProbability { get; set; }   // Derived from odds
    public float ImpliedAwayProbability { get; set; }
    
    // === TIME/SEASONAL FEATURES ===
    
    public float DayOfWeek { get; set; }                // 0-6
    public float IsWeekend { get; set; }                // 1.0 if Sat/Sun
    public float Month { get; set; }                    // 1-12
    public float DaysSinceLastMatchHome { get; set; }   // Rest days
    public float DaysSinceLastMatchAway { get; set; }

    // === xG FEATURES (Understat) ===

    public float HomeXgPerMatch { get; set; }          // Team's xG per match this season
    public float HomeXgAgainstPerMatch { get; set; }   // Team's xGA per match
    public float HomeXgOverperformance { get; set; }   // Goals - xG (positive = overperforming)
    public float HomeRecentXgPerMatch { get; set; }    // xG per match last 5 games

    public float AwayXgPerMatch { get; set; }
    public float AwayXgAgainstPerMatch { get; set; }
    public float AwayXgOverperformance { get; set; }
    public float AwayRecentXgPerMatch { get; set; }

    // === ELO RATING FEATURES (ClubElo) ===

    public float HomeEloRating { get; set; }           // e.g., 1843
    public float AwayEloRating { get; set; }           // e.g., 1720
    public float EloDifference { get; set; }           // Home - Away

    // === MATCH STATS FEATURES (Football-Data.co.uk CSV) ===

    public float HomeShotsPerMatch { get; set; }       // Season avg
    public float HomeShotsOnTargetPerMatch { get; set; }
    public float HomeSOTRatio { get; set; }            // Shots on target / total shots

    public float AwayShotsPerMatch { get; set; }
    public float AwayShotsOnTargetPerMatch { get; set; }
    public float AwaySOTRatio { get; set; }

    // === INJURY FEATURES (API-Football, optional) ===

    public float HomeInjuryImpactScore { get; set; }   // 0-100
    public float HomeKeyPlayersInjured { get; set; }   // Count
    public float AwayInjuryImpactScore { get; set; }
    public float AwayKeyPlayersInjured { get; set; }
}
```

### 2.2 ScorePrediction Output Class

**File**: `src/ExtraTime.Application/Features/ML/Models/ScorePrediction.cs`

```csharp
namespace ExtraTime.Application.Features.ML.Models;

/// <summary>
/// ML.NET model output for score prediction.
/// Multi-output regression: predicts both scores simultaneously.
/// </summary>
public sealed class ScorePrediction
{
    // Home team predicted goals (0-5 range)
    [ColumnName("ScoreHome")]
    public float ScoreHome { get; set; }
    
    // Away team predicted goals (0-5 range)
    [ColumnName("ScoreAway")]
    public float ScoreAway { get; set; }
    
    // Rounded scores for actual predictions
    public int PredictedHomeScore => Math.Clamp((int)Math.Round(ScoreHome), 0, 5);
    public int PredictedAwayScore => Math.Clamp((int)Math.Round(ScoreAway), 0, 5);
    
    // Confidence metrics
    public float HomeConfidence { get; set; }  // Could use uncertainty quantification
    public float AwayConfidence { get; set; }
    
    // Derived prediction
    public string PredictedOutcome => 
        PredictedHomeScore > PredictedAwayScore ? "HomeWin" :
        PredictedHomeScore == PredictedAwayScore ? "Draw" : 
        "AwayWin";
    
    public string PredictedScore => $"{PredictedHomeScore}-{PredictedAwayScore}";
}
```

### 2.3 IMlFeatureExtractor Interface

**File**: `src/ExtraTime.Application/Features/ML/Services/IMlFeatureExtractor.cs`

```csharp
namespace ExtraTime.Application.Features.ML.Services;

public interface IMlFeatureExtractor
{
    /// <summary>
    /// Extract features for a specific match.
    /// </summary>
    Task<MatchFeatures> ExtractFeaturesAsync(
        Guid matchId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extract features for multiple matches efficiently.
    /// </summary>
    Task<List<MatchFeatures>> ExtractFeaturesBatchAsync(
        List<Guid> matchIds,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get training data: completed matches with features and actual scores.
    /// </summary>
    Task<List<(MatchFeatures Features, int ActualHomeScore, int ActualAwayScore)>> 
        GetTrainingDataAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? league = null,
            CancellationToken cancellationToken = default);
}
```

### 2.4 MlFeatureExtractor Implementation

**File**: `src/ExtraTime.Infrastructure/Services/MlFeatureExtractor.cs`

```csharp
namespace ExtraTime.Infrastructure.Services;

public sealed class MlFeatureExtractor(
    IApplicationDbContext context,
    ILogger<MlFeatureExtractor> logger) : IMlFeatureExtractor
{
    public async Task<MatchFeatures> ExtractFeaturesAsync(
        Guid matchId, 
        CancellationToken cancellationToken = default)
    {
        var match = await context.Matches
            .AsNoTracking()
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.League)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
            
        if (match == null)
            throw new NotFoundException($"Match {matchId} not found");
            
        return await ExtractFeaturesInternalAsync(match, cancellationToken);
    }
    
    private async Task<MatchFeatures> ExtractFeaturesInternalAsync(
        Match match, 
        CancellationToken cancellationToken)
    {
        var features = new MatchFeatures
        {
            MatchId = match.Id.ToString(),
            HomeTeamId = match.HomeTeamId.ToString(),
            AwayTeamId = match.AwayTeamId.ToString(),
        };
        
        // Get form data for home team
        var homeForm = await GetTeamFormAsync(
            match.HomeTeamId, 
            match.LeagueId, 
            match.MatchDate,
            isHomeTeam: true,
            cancellationToken);
            
        // Get form data for away team  
        var awayForm = await GetTeamFormAsync(
            match.AwayTeamId,
            match.LeagueId,
            match.MatchDate,
            isHomeTeam: false,
            cancellationToken);
            
        // Get H2H data
        var h2hData = await GetHeadToHeadAsync(
            match.HomeTeamId,
            match.AwayTeamId,
            match.MatchDate,
            cancellationToken);
            
        // Get league context
        var leagueContext = await GetLeagueContextAsync(
            match.LeagueId,
            match.Season,
            match.MatchDate,
            cancellationToken);
            
        // Populate features
        PopulateFormFeatures(features, homeForm, awayForm);
        PopulateH2HFeatures(features, h2hData);
        PopulateLeagueFeatures(features, leagueContext);
        
        // Odds features (if available in match or external API)
        PopulateOddsFeatures(features, match);
        
        // Temporal features
        PopulateTemporalFeatures(features, match);

        // xG features (Phase 9.5 - Understat)
        await PopulateXgFeaturesAsync(features, match, cancellationToken);

        // Elo features (Phase 9.5 - ClubElo)
        await PopulateEloFeaturesAsync(features, match, cancellationToken);

        // Shot stats features (Phase 9.5 - Football-Data.co.uk)
        await PopulateShotStatsFeaturesAsync(features, match, cancellationToken);

        // Injury features (Phase 9.5 - API-Football, optional)
        await PopulateInjuryFeaturesAsync(features, match, cancellationToken);

        return features;
    }
    
    private async Task<TeamFormData> GetTeamFormAsync(
        Guid teamId,
        Guid leagueId,
        DateTime beforeDate,
        bool isHomeTeam,
        CancellationToken cancellationToken)
    {
        // Get last N completed matches before this date
        var recentMatches = await context.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished
                && m.MatchDate < beforeDate
                && (m.HomeTeamId == teamId || m.AwayTeamId == teamId)
                && m.LeagueId == leagueId
                && m.Season == GetSeason(beforeDate))
            .OrderByDescending(m => m.MatchDate)
            .Take(10)
            .ToListAsync(cancellationToken);
            
        var formData = new TeamFormData();
        
        foreach (var m in recentMatches.Take(5))
        {
            var isHome = m.HomeTeamId == teamId;
            var teamScore = isHome ? m.HomeScore : m.AwayScore;
            var opponentScore = isHome ? m.AwayScore : m.HomeScore;
            
            if (!teamScore.HasValue || !opponentScore.HasValue) continue;
            
            formData.Last5GoalsScored += teamScore.Value;
            formData.Last5GoalsConceded += opponentScore.Value;
            formData.Last5CleanSheets += opponentScore.Value == 0 ? 1 : 0;
            
            var result = teamScore > opponentScore ? "W" :
                teamScore == opponentScore ? "D" : "L";
                
            switch (result)
            {
                case "W": formData.Last5Wins++; formData.Last5Points += 3; break;
                case "D": formData.Last5Draws++; formData.Last5Points += 1; break;
                case "L": formData.Last5Losses++; break;
            }
            
            // Home/Away specific stats
            if (isHomeTeam && isHome)
            {
                formData.HomeGoalsScored += teamScore.Value;
                formData.HomeGoalsConceded += opponentScore.Value;
                formData.HomeMatches++;
                if (result == "W") formData.HomeWins++;
            }
            else if (!isHomeTeam && !isHome)
            {
                formData.AwayGoalsScored += teamScore.Value;
                formData.AwayGoalsConceded += opponentScore.Value;
                formData.AwayMatches++;
                if (result == "W") formData.AwayWins++;
            }
        }
        
        // Season averages (all matches)
        var seasonMatches = await context.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished
                && m.MatchDate < beforeDate
                && (m.HomeTeamId == teamId || m.AwayTeamId == teamId)
                && m.LeagueId == leagueId
                && m.Season == GetSeason(beforeDate))
            .ToListAsync(cancellationToken);
            
        foreach (var m in seasonMatches)
        {
            var isHome = m.HomeTeamId == teamId;
            var teamScore = isHome ? m.HomeScore : m.AwayScore;
            var opponentScore = isHome ? m.AwayScore : m.HomeScore;
            
            if (!teamScore.HasValue || !opponentScore.HasValue) continue;
            
            formData.SeasonGoalsScored += teamScore.Value;
            formData.SeasonGoalsConceded += opponentScore.Value;
            formData.SeasonMatches++;
            
            if (teamScore > opponentScore) formData.SeasonPoints += 3;
            else if (teamScore == opponentScore) formData.SeasonPoints += 1;
        }
        
        // Get league position
        formData.LeaguePosition = await CalculateLeaguePositionAsync(
            teamId, leagueId, beforeDate, cancellationToken);
            
        return formData;
    }
    
    // Additional helper methods...
}
```

---

## Part 3: Trainer Console Application

### 3.1 Create New Project

**Project**: `src/ExtraTime.MLTrainer/ExtraTime.MLTrainer.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.ML" Version="3.0.1" />
    <PackageReference Include="Microsoft.ML.FastTree" Version="3.0.1" />
    <PackageReference Include="Microsoft.ML.Trainers" Version="3.0.1" />
    <PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Serilog" Version="3.1.1" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../ExtraTime.Application/ExtraTime.Application.csproj" />
    <ProjectReference Include="../ExtraTime.Infrastructure/ExtraTime.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### 3.2 Model Trainer Service

**File**: `src/ExtraTime.MLTrainer/Services/ModelTrainer.cs`

```csharp
namespace ExtraTime.MLTrainer.Services;

public sealed class ModelTrainer(
    IMlFeatureExtractor featureExtractor,
    IApplicationDbContext context,
    IBlobStorageService blobStorage,
    ILogger<ModelTrainer> logger)
{
    private readonly MLContext _mlContext = new(seed: 42);
    
    /// <summary>
    /// Train models for a specific league and season.
    /// </summary>
    public async Task<TrainingResult> TrainAsync(
        string league,
        int season,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Starting model training for {League} season {Season}", 
            league, season);
            
        // Get training data
        var trainingData = await featureExtractor.GetTrainingDataAsync(
            fromDate,
            DateTime.UtcNow.AddDays(-1), // Exclude today's matches
            league,
            cancellationToken);
            
        if (trainingData.Count < 100)
        {
            throw new InvalidOperationException(
                $"Insufficient training data: {trainingData.Count} samples");
        }
        
        logger.LogInformation("Retrieved {Count} training samples", trainingData.Count);
        
        // Convert to ML.NET data views
        var trainingExamples = trainingData.Select(d => new TrainingExample
        {
            // Features
            HomeFormPointsLast5 = d.Features.HomeFormPointsLast5,
            HomeGoalsScoredLast5 = d.Features.HomeGoalsScoredLast5,
            // ... all features
            
            // Labels (actual scores)
            ActualHomeScore = (float)d.ActualHomeScore,
            ActualAwayScore = (float)d.ActualAwayScore
        }).ToList();
        
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingExamples);
        
        // Split data
        var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        var trainData = trainTestSplit.TrainSet;
        var testData = trainTestSplit.TestSet;
        
        // Train separate models for home and away scores
        var homeModelResult = await TrainScoreModelAsync(
            trainData, 
            testData, 
            "ActualHomeScore",
            "ScoreHome",
            cancellationToken);
            
        var awayModelResult = await TrainScoreModelAsync(
            trainData,
            testData,
            "ActualAwayScore", 
            "ScoreAway",
            cancellationToken);
            
        // Save models to blob storage
        var version = $"v{DateTime.UtcNow:yyyy-MM-dd}-{DateTime.UtcNow:HHmm}";
        
        var homeBlobPath = await SaveModelAsync(
            homeModelResult.Model, 
            league, 
            "HomeScore", 
            version,
            cancellationToken);
            
        var awayBlobPath = await SaveModelAsync(
            awayModelResult.Model,
            league,
            "AwayScore",
            version, 
            cancellationToken);
            
        // Save model metadata to database
        await SaveModelVersionAsync(
            "HomeScore",
            version,
            homeBlobPath,
            homeModelResult,
            trainingData.Count,
            cancellationToken);
            
        await SaveModelVersionAsync(
            "AwayScore", 
            version,
            awayBlobPath,
            awayModelResult,
            trainingData.Count,
            cancellationToken);
            
        logger.LogInformation(
            "Model training completed. Version: {Version}", 
            version);
            
        return new TrainingResult
        {
            Version = version,
            TrainingSamples = trainingData.Count,
            HomeModelMetrics = homeModelResult.Metrics,
            AwayModelMetrics = awayModelResult.Metrics
        };
    }
    
    private async Task<ModelTrainingResult> TrainScoreModelAsync(
        IDataView trainData,
        IDataView testData,
        string labelColumn,
        string outputColumn,
        CancellationToken cancellationToken)
    {
        // Build feature columns list
        var featureColumns = typeof(MatchFeatures)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(float))
            .Select(p => p.Name)
            .Where(n => n != "MatchId" && n != "HomeTeamId" && n != "AwayTeamId")
            .ToArray();
            
        // Create pipeline
        var pipeline = _mlContext.Transforms
            .Concatenate("Features", featureColumns)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: labelColumn,
                featureColumnName: "Features",
                numberOfLeaves: 50,
                numberOfTrees: 500,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.2));
                
        // Train model
        logger.LogInformation("Training {Label} model...", labelColumn);
        var model = pipeline.Fit(trainData);
        
        // Evaluate
        var predictions = model.Transform(testData);
        var metrics = _mlContext.Regression.Evaluate(
            predictions, 
            labelColumnName: labelColumn,
            scoreColumnName: outputColumn);
            
        logger.LogInformation(
            "{Label} Model - R²: {RSquared:F3}, MAE: {MAE:F3}, RMSE: {RMSE:F3}",
            labelColumn,
            metrics.RSquared,
            metrics.MeanAbsoluteError,
            metrics.RootMeanSquaredError);
            
        return new ModelTrainingResult
        {
            Model = model,
            Metrics = metrics
        };
    }
    
    private async Task<string> SaveModelAsync(
        ITransformer model,
        string league,
        string modelType,
        string version,
        CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{modelType}_{version}.zip");
        _mlContext.Model.Save(model, null, tempPath);
        
        var blobName = $"ml-models/{league}/{modelType}/{version}.zip";
        var blobPath = await blobStorage.UploadAsync(
            tempPath, 
            blobName, 
            cancellationToken);
            
        File.Delete(tempPath);
        
        return blobPath;
    }
}
```

---

## Part 4: Prediction Service

### 4.1 IMlPredictionService Interface

**File**: `src/ExtraTime.Application/Features/ML/Services/IMlPredictionService.cs`

```csharp
namespace ExtraTime.Application.Features.ML.Services;

public interface IMlPredictionService
{
    /// <summary>
    /// Predict scores for a single match.
    /// </summary>
    Task<ScorePrediction> PredictScoresAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Predict scores for multiple matches.
    /// </summary>
    Task<List<ScorePrediction>> PredictScoresBatchAsync(
        List<Guid> matchIds,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current active model version.
    /// </summary>
    Task<string?> GetActiveModelVersionAsync(
        string modelType = "HomeScore",
        CancellationToken cancellationToken = default);
}
```

### 4.2 MlPredictionService Implementation

**File**: `src/ExtraTime.Infrastructure/Services/MlPredictionService.cs`

```csharp
namespace ExtraTime.Infrastructure.Services;

public sealed class MlPredictionService : IMlPredictionService
{
    private readonly IMlFeatureExtractor _featureExtractor;
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<MlPredictionService> _logger;
    private readonly MLContext _mlContext = new(seed: 42);
    
    // Model cache
    private ITransformer? _homeScoreModel;
    private ITransformer? _awayScoreModel;
    private string? _cachedModelVersion;
    private readonly SemaphoreSlim _modelLock = new(1, 1);
    
    public MlPredictionService(
        IMlFeatureExtractor featureExtractor,
        IApplicationDbContext context,
        IBlobStorageService blobStorage,
        ILogger<MlPredictionService> logger)
    {
        _featureExtractor = featureExtractor;
        _context = context;
        _blobStorage = blobStorage;
        _logger = logger;
    }
    
    public async Task<ScorePrediction> PredictScoresAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        // Load models if not cached
        await EnsureModelsLoadedAsync(cancellationToken);
        
        // Extract features
        var features = await _featureExtractor.ExtractFeaturesAsync(
            matchId, 
            cancellationToken);
            
        // Create prediction engine
        var homeEngine = _mlContext.Model
            .CreatePredictionEngine<MatchFeatures, ScorePrediction>(_homeScoreModel!);
            
        var awayEngine = _mlContext.Model
            .CreatePredictionEngine<MatchFeatures, ScorePrediction>(_awayScoreModel!);
            
        // Predict
        var homePrediction = homeEngine.Predict(features);
        var awayPrediction = awayEngine.Predict(features);
        
        return new ScorePrediction
        {
            ScoreHome = homePrediction.ScoreHome,
            ScoreAway = awayPrediction.ScoreAway
        };
    }
    
    private async Task EnsureModelsLoadedAsync(CancellationToken cancellationToken)
    {
        // Get active model version
        var activeVersion = await _context.MlModelVersions
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => m.Version)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (string.IsNullOrEmpty(activeVersion))
        {
            throw new InvalidOperationException("No active ML model found");
        }
        
        // Check if we need to reload
        if (_cachedModelVersion != activeVersion || _homeScoreModel == null)
        {
            await _modelLock.WaitAsync(cancellationToken);
            try
            {
                if (_cachedModelVersion != activeVersion || _homeScoreModel == null)
                {
                    await LoadModelsAsync(activeVersion, cancellationToken);
                    _cachedModelVersion = activeVersion;
                }
            }
            finally
            {
                _modelLock.Release();
            }
        }
    }
    
    private async Task LoadModelsAsync(
        string version, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading ML models version {Version}", version);
        
        // Load home score model
        var homeModelVersion = await _context.MlModelVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Version == version && m.ModelType == "HomeScore",
                cancellationToken);
                
        if (homeModelVersion?.BlobPath != null)
        {
            var homeModelBytes = await _blobStorage.DownloadAsync(
                homeModelVersion.BlobPath, 
                cancellationToken);
            using var homeStream = new MemoryStream(homeModelBytes);
            _homeScoreModel = _mlContext.Model.Load(homeStream, out _);
        }
        
        // Load away score model
        var awayModelVersion = await _context.MlModelVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Version == version && m.ModelType == "AwayScore",
                cancellationToken);
                
        if (awayModelVersion?.BlobPath != null)
        {
            var awayModelBytes = await _blobStorage.DownloadAsync(
                awayModelVersion.BlobPath,
                cancellationToken);
            using var awayStream = new MemoryStream(awayModelBytes);
            _awayScoreModel = _mlContext.Model.Load(awayStream, out _);
        }
        
        _logger.LogInformation("ML models loaded successfully");
    }
}
```

---

## Part 5: Bot Strategy Integration

### 5.1 MachineLearningStrategy

**File**: `src/ExtraTime.Infrastructure/Services/BotStrategies/MachineLearningStrategy.cs`

```csharp
namespace ExtraTime.Infrastructure.Services.BotStrategies;

public sealed class MachineLearningStrategy(
    IMlPredictionService predictionService,
    ILogger<MachineLearningStrategy> logger) : IBotStrategy
{
    public BotStrategy Strategy => BotStrategy.MachineLearning;
    
    public string Description => "Uses ML.NET trained models to predict exact scores";
    
    public async Task<PredictionResult> PredictAsync(
        Guid matchId,
        BotPersonality personality,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get ML prediction
            var prediction = await predictionService.PredictScoresAsync(
                matchId, 
                cancellationToken);
                
            // Apply personality modifiers (risk adjustment)
            var (adjustedHome, adjustedAway) = ApplyPersonalityModifiers(
                prediction.PredictedHomeScore,
                prediction.PredictedAwayScore,
                personality);
                
            logger.LogDebug(
                "ML Strategy: Raw {RawScore}, Adjusted {AdjustedScore} for match {MatchId}",
                prediction.PredictedScore,
                $"{adjustedHome}-{adjustedAway}",
                matchId);
                
            return new PredictionResult
            {
                PredictedHomeScore = adjustedHome,
                PredictedAwayScore = adjustedAway,
                Confidence = CalculateConfidence(prediction, personality),
                Reasoning = $"ML model predicts {prediction.PredictedScore} " +
                    $"(raw: {prediction.ScoreHome:F1}-{prediction.ScoreAway:F1}). " +
                    GetPersonalityReasoning(personality)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "ML prediction failed for match {MatchId}, falling back to Statistical", 
                matchId);
                
            // Fallback to statistical strategy
            return await new StatisticalStrategy(logger)
                .PredictAsync(matchId, personality, cancellationToken);
        }
    }
    
    private (int home, int away) ApplyPersonalityModifiers(
        int rawHome, 
        int rawAway, 
        BotPersonality personality)
    {
        return personality.RiskProfile switch
        {
            "conservative" => (Math.Max(0, rawHome - 1), Math.Max(0, rawAway - 1)),
            "aggressive" => (
                Math.Min(5, rawHome + (rawHome < 2 ? 1 : 0)),
                Math.Min(5, rawAway + (rawAway < 2 ? 1 : 0))
            ),
            _ => (rawHome, rawAway) // balanced - no adjustment
        };
    }
    
    private double CalculateConfidence(ScorePrediction prediction, BotPersonality personality)
    {
        // Confidence based on prediction certainty
        var scoreC = 1.0 - (Math.Abs(prediction.ScoreHome - prediction.PredictedHomeScore) +
            Math.Abs(prediction.ScoreAway - prediction.PredictedAwayScore)) / 10.0;
            
        // Apply personality confidence modifier
        return Math.Clamp(scoreC * personality.BaseConfidence, 0.3, 0.95);
    }
    
    private string GetPersonalityReasoning(BotPersonality personality)
    {
        return personality.RiskProfile switch
        {
            "conservative" => "Adjusted down for conservative approach.",
            "aggressive" => "Adjusted up for aggressive high-scoring predictions.",
            _ => "Using raw ML predictions."
        };
    }
}
```

### 5.2 Register Strategy

Update `StrategyFactory` to include ML strategy:

**File**: `src/ExtraTime.Infrastructure/Services/BotStrategies/StrategyFactory.cs`

```csharp
public sealed class StrategyFactory(IServiceProvider serviceProvider) : IStrategyFactory
{
    private readonly Dictionary<BotStrategy, IBotStrategy> _strategies = new();
    
    public IBotStrategy GetStrategy(BotStrategy strategy)
    {
        if (_strategies.TryGetValue(strategy, out var cached))
            return cached;
            
        IBotStrategy instance = strategy switch
        {
            BotStrategy.Random => new RandomStrategy(),
            BotStrategy.HomeBias => new HomeBiasStrategy(),
            BotStrategy.Conservative => new ConservativeStrategy(),
            BotStrategy.HighScoring => new HighScoringStrategy(),
            BotStrategy.Statistical => serviceProvider.GetRequiredService<StatisticalStrategy>(),
            BotStrategy.FormBased => serviceProvider.GetRequiredService<FormBasedStrategy>(),
            BotStrategy.MachineLearning => serviceProvider.GetRequiredService<MachineLearningStrategy>(),
            BotStrategy.Hybrid => serviceProvider.GetRequiredService<HybridStrategy>(),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy))
        };
        
        _strategies[strategy] = instance;
        return instance;
    }
}
```

---

## Part 6: Azure Function for Predictions

### 6.1 Create BotFunctions Project

**Project**: `src/ExtraTime.BotFunctions/ExtraTime.BotFunctions.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AzureFunctionsVersion>v4</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.20.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Timer" Version="1.0.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.16.0" />
    <PackageReference Include="Microsoft.ML" Version="3.0.1" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../ExtraTime.Application/ExtraTime.Application.csproj" />
    <ProjectReference Include="../ExtraTime.Infrastructure/ExtraTime.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### 6.2 PlaceBotBetsFunction

**File**: `src/ExtraTime.BotFunctions/Functions/PlaceBotBetsFunction.cs`

```csharp
namespace ExtraTime.BotFunctions.Functions;

public sealed class PlaceBotBetsFunction(
    IBotService botService,
    IApplicationDbContext context,
    ILogger<PlaceBotBetsFunction> logger)
{
    [Function("PlaceBotBets")]
    public async Task RunAsync(
        [TimerTrigger("0 0 6 * * 1-5")] TimerInfo timerInfo,  // 6 AM Mon-Fri
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting automated bot bet placement at {Time}", 
            DateTime.UtcNow);
            
        // Get today's scheduled matches
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var scheduledMatches = await context.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Scheduled
                && m.MatchDate >= today
                && m.MatchDate < tomorrow)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
            
        logger.LogInformation("Found {Count} matches for today", scheduledMatches.Count);
        
        // Get all active ML bots
        var mlBots = await context.Bots
            .AsNoTracking()
            .Where(b => b.IsActive 
                && b.Strategy == BotStrategy.MachineLearning)
            .ToListAsync(cancellationToken);
            
        logger.LogInformation("Found {Count} ML bots", mlBots.Count);
        
        // Place bets for each bot on each match
        foreach (var matchId in scheduledMatches)
        {
            foreach (var bot in mlBots)
            {
                try
                {
                    var result = await botService.PlaceBotBetAsync(
                        bot.Id,
                        matchId,
                        cancellationToken);
                        
                    if (result.IsSuccess)
                    {
                        logger.LogInformation(
                            "Bot {BotName} placed bet on match {MatchId}: {Score}",
                            bot.Name,
                            matchId,
                            result.Value?.PredictedScore);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Bot {BotName} failed to bet on {MatchId}: {Error}",
                            bot.Name,
                            matchId,
                            result.Error);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error placing bet for bot {BotId} on match {MatchId}",
                        bot.Id,
                        matchId);
                }
            }
        }
        
        logger.LogInformation("Bot bet placement completed");
    }
}
```

---

## Part 7: Admin Dashboard

### 7.1 Admin ML Endpoints

**File**: `src/ExtraTime.API/Endpoints/AdminMlEndpoints.cs`

```csharp
namespace ExtraTime.API.Endpoints;

public static class AdminMlEndpoints
{
    public static IEndpointRouteBuilder MapAdminMlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/ml")
            .WithTags("ML Admin")
            .WithName("ML Admin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
            
        // Get model versions
        group.MapGet("/models", GetModelVersionsAsync)
            .WithName("GetMlModelVersions");
            
        // Activate model version
        group.MapPost("/models/{version}/activate", ActivateModelAsync)
            .WithName("ActivateMlModel");
            
        // Trigger training
        group.MapPost("/train", TriggerTrainingAsync)
            .WithName("TriggerMlTraining");
            
        // Get prediction accuracy comparison
        group.MapGet("/accuracy", GetAccuracyComparisonAsync)
            .WithName("GetAccuracyComparison");
            
        // Get feature importance
        group.MapGet("/models/{version}/features", GetFeatureImportanceAsync)
            .WithName("GetFeatureImportance");
            
        return app;
    }
    
    private static async Task<IResult> GetModelVersionsAsync(
        IApplicationDbContext context,
        CancellationToken ct)
    {
        var models = await context.MlModelVersions
            .AsNoTracking()
            .OrderByDescending(m => m.TrainedAt)
            .Select(m => new
            {
                m.Id,
                m.ModelType,
                m.Version,
                m.IsActive,
                m.TrainedAt,
                m.TrainingSamples,
                m.Rsquared,
                m.MeanAbsoluteError,
                m.AlgorithmUsed
            })
            .ToListAsync(ct);
            
        return Results.Ok(models);
    }
    
    private static async Task<IResult> ActivateModelAsync(
        string version,
        ActivateModelRequest request,
        IApplicationDbContext context,
        CancellationToken ct)
    {
        // Deactivate current active models
        var activeModels = await context.MlModelVersions
            .Where(m => m.IsActive)
            .ToListAsync(ct);
            
        foreach (var model in activeModels)
        {
            model.IsActive = false;
        }
        
        // Activate new version
        var newModel = await context.MlModelVersions
            .FirstOrDefaultAsync(m => m.Version == version, ct);
            
        if (newModel == null)
            return Results.NotFound(new { error = "Model version not found" });
            
        newModel.IsActive = true;
        newModel.ActivatedAt = DateTime.UtcNow;
        newModel.ActivationNotes = request.Notes;
        
        await context.SaveChangesAsync(ct);
        
        return Results.Ok(new { message = "Model activated successfully" });
    }
    
    private static async Task<IResult> GetAccuracyComparisonAsync(
        string? period = "monthly",
        IApplicationDbContext context,
        CancellationToken ct)
    {
        var accuracies = await context.BotPredictionAccuracies
            .AsNoTracking()
            .Where(a => a.PeriodType == period)
            .Include(a => a.Bot)
            .OrderByDescending(a => a.PeriodEnd)
            .GroupBy(a => a.Strategy)
            .Select(g => new
            {
                Strategy = g.Key.ToString(),
                AvgExactAccuracy = g.Average(a => a.ExactScoreAccuracy),
                AvgResultAccuracy = g.Average(a => a.CorrectResultAccuracy),
                AvgMAE = g.Average(a => a.MeanAbsoluteError),
                TotalPredictions = g.Sum(a => a.TotalPredictions),
                LatestPeriod = g.Max(a => a.PeriodEnd)
            })
            .ToListAsync(ct);
            
        return Results.Ok(accuracies);
    }
}
```

### 7.2 Prediction Accuracy Tracker

**File**: `src/ExtraTime.Infrastructure/Services/PredictionAccuracyTracker.cs`

```csharp
namespace ExtraTime.Infrastructure.Services;

public sealed class PredictionAccuracyTracker(
    IApplicationDbContext context,
    ILogger<PredictionAccuracyTracker> logger)
{
    /// <summary>
    /// Recalculate accuracy metrics for all bots after match results.
    /// Should be called periodically (e.g., daily after matches complete).
    /// </summary>
    public async Task RecalculateAccuracyAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Recalculating prediction accuracy from {From} to {To}",
            fromDate,
            toDate);
            
        // Get all completed matches in period with bot bets
        var matchesWithBets = await context.BotBets
            .AsNoTracking()
            .Include(b => b.Bet)
            .Include(b => b.Bot)
            .Include(b => b.Bet.Match)
            .Where(b => b.Bet.Match.Status == MatchStatus.Finished
                && b.Bet.Match.MatchDate >= fromDate
                && b.Bet.Match.MatchDate < toDate)
            .ToListAsync(cancellationToken);
            
        // Group by bot and strategy
        var grouped = matchesWithBets.GroupBy(b => new { b.BotId, b.Bot.Strategy });
        
        foreach (var group in grouped)
        {
            var botId = group.Key.BotId;
            var strategy = group.Key.Strategy;
            
            var accuracy = CalculateAccuracy(group.ToList());
            
            // Save or update accuracy record
            var existing = await context.BotPredictionAccuracies
                .FirstOrDefaultAsync(
                    a => a.BotId == botId 
                        && a.PeriodType == "custom"
                        && a.PeriodStart == fromDate
                        && a.PeriodEnd == toDate,
                    cancellationToken);
                    
            if (existing == null)
            {
                existing = new BotPredictionAccuracy
                {
                    BotId = botId,
                    Strategy = strategy,
                    PeriodStart = fromDate,
                    PeriodEnd = toDate,
                    PeriodType = "custom",
                    LastUpdatedAt = DateTime.UtcNow
                };
                context.BotPredictionAccuracies.Add(existing);
            }
            
            // Update metrics
            existing.TotalPredictions = accuracy.TotalPredictions;
            existing.ExactScores = accuracy.ExactScores;
            existing.CorrectResults = accuracy.CorrectResults;
            existing.GoalsOffBy1 = accuracy.GoalsOffBy1;
            existing.GoalsOffBy2 = accuracy.GoalsOffBy2;
            existing.GoalsOffBy3Plus = accuracy.GoalsOffBy3Plus;
            existing.ExactScoreAccuracy = accuracy.ExactScoreAccuracy;
            existing.CorrectResultAccuracy = accuracy.CorrectResultAccuracy;
            existing.MeanAbsoluteError = accuracy.MAE;
            existing.LastUpdatedAt = DateTime.UtcNow;
        }
        
        await context.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Prediction accuracy recalculation completed");
    }
    
    private AccuracyMetrics CalculateAccuracy(List<BotBet> bets)
    {
        var metrics = new AccuracyMetrics();
        
        foreach (var bet in bets)
        {
            var match = bet.Bet.Match;
            if (!match.HomeScore.HasValue || !match.AwayScore.HasValue) continue;
            
            var actualHome = match.HomeScore.Value;
            var actualAway = match.AwayScore.Value;
            var predictedHome = bet.PredictedHomeScore;
            var predictedAway = bet.PredictedAwayScore;
            
            metrics.TotalPredictions++;
            
            // Exact score
            if (actualHome == predictedHome && actualAway == predictedAway)
                metrics.ExactScores++;
                
            // Correct result
            var actualResult = actualHome > actualAway ? "H" : 
                actualHome == actualAway ? "D" : "A";
            var predictedResult = predictedHome > predictedAway ? "H" :
                predictedHome == predictedAway ? "D" : "A";
                
            if (actualResult == predictedResult)
                metrics.CorrectResults++;
                
            // Goal difference
            var totalDiff = Math.Abs(actualHome - predictedHome) + 
                Math.Abs(actualAway - predictedAway);
                
            switch (totalDiff)
            {
                case 1: metrics.GoalsOffBy1++; break;
                case 2: metrics.GoalsOffBy2++; break;
                default: 
                    if (totalDiff >= 3) metrics.GoalsOffBy3Plus++; 
                    break;
            }
            
            // MAE components
            metrics.TotalAbsoluteError += totalDiff;
            metrics.HomeScoreError += Math.Abs(actualHome - predictedHome);
            metrics.AwayScoreError += Math.Abs(actualAway - predictedAway);
        }
        
        if (metrics.TotalPredictions > 0)
        {
            metrics.ExactScoreAccuracy = (double)metrics.ExactScores / metrics.TotalPredictions;
            metrics.CorrectResultAccuracy = (double)metrics.CorrectResults / metrics.TotalPredictions;
            metrics.MAE = (double)metrics.TotalAbsoluteError / metrics.TotalPredictions / 2.0;
        }
        
        return metrics;
    }
}
```

---

## Part 8: Frontend Components

### 8.1 ML Model Admin Page

**File**: `frontend/pages/admin/ml-models.tsx`

```tsx
import { useState, useEffect } from 'react';
import { 
  Card, CardContent, CardHeader, CardTitle,
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
  Button, Badge, Select
} from '@/components/ui';

interface MlModelVersion {
  id: string;
  modelType: string;
  version: string;
  isActive: boolean;
  trainedAt: string;
  trainingSamples: number;
  rsquared: number;
  meanAbsoluteError: number;
  algorithmUsed: string;
}

export default function MLModelsAdminPage() {
  const [models, setModels] = useState<MlModelVersion[]>([]);
  const [loading, setLoading] = useState(true);
  const [accuracyData, setAccuracyData] = useState<any[]>([]);

  useEffect(() => {
    fetchModels();
    fetchAccuracyComparison();
  }, []);

  const fetchModels = async () => {
    const response = await fetch('/api/admin/ml/models');
    const data = await response.json();
    setModels(data);
    setLoading(false);
  };

  const fetchAccuracyComparison = async () => {
    const response = await fetch('/api/admin/ml/accuracy');
    const data = await response.json();
    setAccuracyData(data);
  };

  const activateModel = async (version: string) => {
    const response = await fetch(`/api/admin/ml/models/${version}/activate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ notes: 'Activated from admin dashboard' })
    });
    
    if (response.ok) {
      fetchModels();
    }
  };

  const triggerTraining = async () => {
    const response = await fetch('/api/admin/ml/train', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ 
        league: 'premier-league',
        season: 2024 
      })
    });
    
    if (response.ok) {
      alert('Training job started');
    }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">ML Model Management</h1>
        <Button onClick={triggerTraining}>Trigger New Training</Button>
      </div>

      {/* Model Versions */}
      <Card>
        <CardHeader>
          <CardTitle>Model Versions</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Version</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Trained</TableHead>
                <TableHead>Samples</TableHead>
                <TableHead>R²</TableHead>
                <TableHead>MAE</TableHead>
                <TableHead>Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {models.map((model) => (
                <TableRow key={model.id}>
                  <TableCell className="font-mono">{model.version}</TableCell>
                  <TableCell>{model.modelType}</TableCell>
                  <TableCell>
                    {model.isActive ? (
                      <Badge className="bg-green-100 text-green-800">Active</Badge>
                    ) : (
                      <Badge variant="secondary">Inactive</Badge>
                    )}
                  </TableCell>
                  <TableCell>{new Date(model.trainedAt).toLocaleDateString()}</TableCell>
                  <TableCell>{model.trainingSamples.toLocaleString()}</TableCell>
                  <TableCell>{model.rsquared.toFixed(3)}</TableCell>
                  <TableCell>{model.meanAbsoluteError.toFixed(2)}</TableCell>
                  <TableCell>
                    {!model.isActive && (
                      <Button 
                        size="sm" 
                        onClick={() => activateModel(model.version)}
                      >
                        Activate
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Strategy Comparison */}
      <Card>
        <CardHeader>
          <CardTitle>Strategy Accuracy Comparison</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Strategy</TableHead>
                <TableHead>Total Predictions</TableHead>
                <TableHead>Exact Score %</TableHead>
                <TableHead>Correct Result %</TableHead>
                <TableHead>MAE</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {accuracyData.map((row) => (
                <TableRow key={row.strategy}>
                  <TableCell className="font-medium">{row.strategy}</TableCell>
                  <TableCell>{row.totalPredictions.toLocaleString()}</TableCell>
                  <TableCell>{(row.avgExactAccuracy * 100).toFixed(1)}%</TableCell>
                  <TableCell>{(row.avgResultAccuracy * 100).toFixed(1)}%</TableCell>
                  <TableCell>{row.avgMAE.toFixed(2)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
```

---

## Part 9: Integration & Testing

### 9.1 Seed ML Bot Personalities

**File**: `src/ExtraTime.Infrastructure/Data/Seeds/MlBotSeed.cs`

```csharp
namespace ExtraTime.Infrastructure.Data.Seeds;

public static class MlBotSeed
{
    public static async Task SeedAsync(IApplicationDbContext context)
    {
        if (await context.Bots.AnyAsync(b => b.Strategy == BotStrategy.MachineLearning))
            return;
            
        var mlBots = new List<Bot>
        {
            new()
            {
                Name = "MLBot-Conservative",
                Description = "ML-powered with conservative risk profile",
                Strategy = BotStrategy.MachineLearning,
                Personality = new BotPersonality
                {
                    RiskProfile = "conservative",
                    ConfidenceThreshold = 0.6,
                    BaseConfidence = 0.7
                },
                IsActive = true
            },
            new()
            {
                Name = "MLBot-Balanced", 
                Description = "ML-powered with balanced approach",
                Strategy = BotStrategy.MachineLearning,
                Personality = new BotPersonality
                {
                    RiskProfile = "balanced",
                    ConfidenceThreshold = 0.5,
                    BaseConfidence = 0.75
                },
                IsActive = true
            },
            new()
            {
                Name = "MLBot-Aggressive",
                Description = "ML-powered favoring higher scores",
                Strategy = BotStrategy.MachineLearning,
                Personality = new BotPersonality
                {
                    RiskProfile = "aggressive",
                    ConfidenceThreshold = 0.4,
                    BaseConfidence = 0.8
                },
                IsActive = true
            }
        };
        
        context.Bots.AddRange(mlBots);
        await context.SaveChangesAsync();
    }
}
```

### 9.2 Test Script

**File**: `tests/ExtraTime.IntegrationTests/ML/MLBotIntegrationTests.cs`

```csharp
namespace ExtraTime.IntegrationTests.ML;

public class MLBotIntegrationTests : TestBase
{
    [Fact]
    public async Task TrainModel_WithHistoricalData_SavesModelToBlob()
    {
        // Arrange
        await SeedHistoricalMatches(100);
        var trainer = GetService<ModelTrainer>();
        
        // Act
        var result = await trainer.TrainAsync("premier-league", 2024);
        
        // Assert
        result.TrainingSamples.Should().BeGreaterThan(50);
        result.HomeModelMetrics.RSquared.Should().BeGreaterThan(0);
        
        // Verify saved to DB
        var modelVersion = await GetService<IApplicationDbContext>()
            .MlModelVersions
            .FirstOrDefaultAsync(m => m.Version == result.Version);
            
        modelVersion.Should().NotBeNull();
        modelVersion.BlobPath.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task PredictScores_WithTrainedModel_ReturnsValidPrediction()
    {
        // Arrange
        await SeedTrainedModel();
        var match = await SeedScheduledMatch();
        var service = GetService<IMlPredictionService>();
        
        // Act
        var prediction = await service.PredictScoresAsync(match.Id);
        
        // Assert
        prediction.ScoreHome.Should().BeInRange(0, 5);
        prediction.ScoreAway.Should().BeInRange(0, 5);
    }
    
    [Fact]
    public async Task MachineLearningStrategy_WithPrediction_PlacesCorrectBet()
    {
        // Arrange
        var match = await SeedScheduledMatch();
        var bot = await SeedMlBot();
        var strategy = GetService<MachineLearningStrategy>();
        
        // Act
        var result = await strategy.PredictAsync(match.Id, bot.Personality);
        
        // Assert
        result.PredictedHomeScore.Should().BeInRange(0, 5);
        result.PredictedAwayScore.Should().BeInRange(0, 5);
        result.Confidence.Should().BeGreaterThan(0);
        result.Reasoning.Should().NotBeNullOrEmpty();
    }
}
```

---

## Implementation Timeline

### Phase 1: Core Infrastructure (8 hours)
- Add MlModelVersion entity and migration
- Add BotPredictionAccuracy entity and migration
- Create MatchFeatures and ScorePrediction classes
- Implement MlFeatureExtractor service

### Phase 2: Local Training (6 hours)
- Create ExtraTime.MLTrainer console app
- Implement ModelTrainer with multiple algorithms
- Test training with existing match data

### Phase 3: Prediction Service (4 hours)
- Implement IMlPredictionService
- Create MlPredictionService
- Implement MachineLearningStrategy

### Phase 4: Azure Functions (4 hours)
- Create ExtraTime.BotFunctions project
- Implement PlaceBotBetsFunction
- Implement RefreshFormCacheFunction

### Phase 5: Admin Dashboard (5 hours)
- Implement AdminMlEndpoints
- Create PredictionAccuracyTracker
- Build frontend admin pages

### Phase 6: Integration & Testing (4 hours)
- Seed ML bot personalities
- Run comparison tests
- Deploy to production

**Total: ~35 hours**

---

## Key Design Decisions

1. **Separate Models for Home/Away**: Train two regression models instead of multi-class classification for better interpretability

2. **Local Training Console**: Training happens on dev machine or Azure VM, not in production web app

3. **Model Versioning**: All models versioned and stored in Blob Storage with metadata in SQL

4. **Hot-swap Models**: Activate new models without restarting the application

5. **Fallback Strategy**: ML strategy falls back to Statistical if prediction fails

6. **Comprehensive Tracking**: Detailed accuracy metrics per strategy for comparison

7. **Feature Engineering**: ~67 features including form, H2H, league context, odds, xG, Elo, shot stats, and injuries

8. **Conservative Scaling**: Clamp predictions to 0-5 range and apply personality modifiers

---

## Success Metrics

- **ML models achieve 15%+ exact score accuracy** (vs 10% random)
- **MAE under 1.2 goals** for both home and away predictions
- **R² > 0.25** indicating meaningful predictive power
- **ML bots outperform** at least 2 of 3 baseline strategies (Random, HomeBias, Conservative)
- **Zero-downtime model updates** via hot-swapping
