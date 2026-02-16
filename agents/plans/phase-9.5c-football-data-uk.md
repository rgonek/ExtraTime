# Phase 9.5C: Football-Data.co.uk Integration - Betting Odds & Match Stats

## Overview
Integrate historical betting odds **and extended match statistics** from Football-Data.co.uk CSV files to provide market consensus data and detailed match stats (shots, corners, cards, referee, half-time scores) for bot prediction strategies.

> **Data Source**: `https://www.football-data.co.uk/mmz4281/{SeasonCode}/{LeagueCode}.csv`
> **Supported Leagues**: Premier League, Championship, La Liga, Bundesliga, Serie A, Ligue 1, Eredivisie, Primeira Liga
> **Sync Strategy**: Weekly on Monday 5 AM UTC
> **Rate Limit**: None (static files)

> **Prerequisite**: Phase 9.5A (Integration Health) must be complete
> **Phase 7.8 Contract**: expose historical/backfill and `asOfUtc` odds retrieval so ML training uses only pre-kickoff-available data.

---

## Part 1: Domain Layer

### 1.1 MatchOdds Entity

**File**: `src/ExtraTime.Domain/Entities/MatchOdds.cs`

```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// Historical betting odds for a match.
/// Data sourced from Football-Data.co.uk CSV files.
/// </summary>
public sealed class MatchOdds : BaseEntity
{
    public required Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    // Average odds across bookmakers
    public double HomeWinOdds { get; set; }      // e.g., 1.85
    public double DrawOdds { get; set; }         // e.g., 3.40
    public double AwayWinOdds { get; set; }      // e.g., 4.50

    // Implied probabilities (calculated from odds)
    public double HomeWinProbability { get; set; }   // e.g., 0.50
    public double DrawProbability { get; set; }      // e.g., 0.27
    public double AwayWinProbability { get; set; }   // e.g., 0.20

    // Over/Under 2.5 goals
    public double? Over25Odds { get; set; }
    public double? Under25Odds { get; set; }

    // Both Teams To Score
    public double? BttsYesOdds { get; set; }
    public double? BttsNoOdds { get; set; }

    // Market consensus (who the market favors)
    public MatchOutcome MarketFavorite { get; set; }
    public double FavoriteConfidence { get; set; }  // 0-1 scale

    // Metadata
    public string DataSource { get; set; } = "football-data.co.uk";
    public DateTime ImportedAt { get; set; }

    // Computed methods
    public static double OddsToProbability(double odds)
    {
        return odds > 0 ? 1.0 / odds : 0;
    }

    public void CalculateProbabilities()
    {
        var total = OddsToProbability(HomeWinOdds) +
                    OddsToProbability(DrawOdds) +
                    OddsToProbability(AwayWinOdds);

        // Normalize to remove bookmaker margin
        HomeWinProbability = OddsToProbability(HomeWinOdds) / total;
        DrawProbability = OddsToProbability(DrawOdds) / total;
        AwayWinProbability = OddsToProbability(AwayWinOdds) / total;

        // Determine market favorite
        if (HomeWinProbability > DrawProbability && HomeWinProbability > AwayWinProbability)
        {
            MarketFavorite = MatchOutcome.HomeWin;
            FavoriteConfidence = HomeWinProbability;
        }
        else if (AwayWinProbability > DrawProbability)
        {
            MarketFavorite = MatchOutcome.AwayWin;
            FavoriteConfidence = AwayWinProbability;
        }
        else
        {
            MarketFavorite = MatchOutcome.Draw;
            FavoriteConfidence = DrawProbability;
        }
    }
}

public enum MatchOutcome
{
    HomeWin = 0,
    Draw = 1,
    AwayWin = 2
}
```

### 1.2 MatchStats Entity (NEW)

**File**: `src/ExtraTime.Domain/Entities/MatchStats.cs`

Separate from `MatchOdds`, this entity stores detailed match statistics extracted from the same CSV source.

```csharp
namespace ExtraTime.Domain.Entities;

public sealed class MatchStats : BaseEntity
{
    public required Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    // Shots
    public int? HomeShots { get; set; }
    public int? HomeShotsOnTarget { get; set; }
    public int? AwayShots { get; set; }
    public int? AwayShotsOnTarget { get; set; }

    // Half-Time
    public int? HomeHalfTimeGoals { get; set; }
    public int? AwayHalfTimeGoals { get; set; }

    // Discipline
    public int? HomeCorners { get; set; }
    public int? AwayCorners { get; set; }
    public int? HomeFouls { get; set; }
    public int? AwayFouls { get; set; }
    public int? HomeYellowCards { get; set; }
    public int? AwayYellowCards { get; set; }
    public int? HomeRedCards { get; set; }
    public int? AwayRedCards { get; set; }

    // Referee
    public string? Referee { get; set; }

    // Metadata
    public string DataSource { get; set; } = "football-data.co.uk";
    public DateTime ImportedAt { get; set; }
}
```

---

## Part 2: Infrastructure Layer

### 2.1 EF Configurations

**File**: `src/ExtraTime.Infrastructure/Data/Configurations/MatchOddsConfiguration.cs`

```csharp
public sealed class MatchOddsConfiguration : IEntityTypeConfiguration<MatchOdds>
{
    public void Configure(EntityTypeBuilder<MatchOdds> builder)
    {
        builder.ToTable("MatchOdds");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.DataSource).HasMaxLength(50);

        builder.HasOne(o => o.Match)
            .WithOne()
            .HasForeignKey<MatchOdds>(o => o.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.MatchId).IsUnique();
    }
}
```

**File**: `src/ExtraTime.Infrastructure/Data/Configurations/MatchStatsConfiguration.cs`

```csharp
public sealed class MatchStatsConfiguration : IEntityTypeConfiguration<MatchStats>
{
    public void Configure(EntityTypeBuilder<MatchStats> builder)
    {
        builder.ToTable("MatchStats");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Referee).HasMaxLength(100);
        builder.Property(s => s.DataSource).HasMaxLength(50);

        builder.HasOne(s => s.Match)
            .WithOne()
            .HasForeignKey<MatchStats>(s => s.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.MatchId).IsUnique();
    }
}
```

### 2.2 ApplicationDbContext

Add to `ApplicationDbContext.cs`:
```csharp
public DbSet<MatchOdds> MatchOdds => Set<MatchOdds>();
public DbSet<MatchStats> MatchStats => Set<MatchStats>();
```

Add to `IApplicationDbContext.cs`:
```csharp
DbSet<MatchOdds> MatchOdds { get; }
DbSet<MatchStats> MatchStats { get; }
```

---

## Part 3: Service Layer

### 3.1 Interface

**File**: `src/ExtraTime.Application/Common/Interfaces/IOddsDataService.cs`

```csharp
public interface IOddsDataService
{
    Task ImportSeasonOddsAsync(
        string leagueCode,
        string season,
        CancellationToken cancellationToken = default);

    Task ImportAllLeaguesAsync(CancellationToken cancellationToken = default);

    Task ImportHistoricalSeasonsAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        CancellationToken cancellationToken = default);

    Task<MatchOdds?> GetOddsForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<MatchOdds?> GetOddsForMatchAsOfAsync(
        Guid matchId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}
```

### 3.2 Extended OddsCsvRow

The CSV row DTO is extended to capture all fields from the football-data.co.uk CSV.

```csharp
internal sealed class OddsCsvRow
{
    public DateTime? Date { get; set; }
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public int? HomeGoals { get; set; }
    public int? AwayGoals { get; set; }
    public double? HomeOdds { get; set; }
    public double? DrawOdds { get; set; }
    public double? AwayOdds { get; set; }
    public double? Over25 { get; set; }
    public double? Under25 { get; set; }

    // NEW - Match Statistics
    public int? HomeHalfTimeGoals { get; set; }   // HTHG
    public int? AwayHalfTimeGoals { get; set; }   // HTAG
    public int? HomeShots { get; set; }            // HS
    public int? HomeShotsOnTarget { get; set; }    // HST
    public int? AwayShots { get; set; }            // AS
    public int? AwayShotsOnTarget { get; set; }    // AST
    public int? HomeCorners { get; set; }          // HC
    public int? AwayCorners { get; set; }          // AC
    public int? HomeFouls { get; set; }            // HF
    public int? AwayFouls { get; set; }            // AF
    public int? HomeYellowCards { get; set; }      // HY
    public int? AwayYellowCards { get; set; }      // AY
    public int? HomeRedCards { get; set; }         // HR
    public int? AwayRedCards { get; set; }         // AR
    public string? Referee { get; set; }           // Referee
}
```

### 3.3 Implementation

**File**: `src/ExtraTime.Infrastructure/Services/ExternalData/OddsDataService.cs`

```csharp
namespace ExtraTime.Infrastructure.Services.ExternalData;

/// <summary>
/// Imports historical betting odds and match statistics from Football-Data.co.uk CSV files.
/// Free data available at: https://www.football-data.co.uk/data.php
/// </summary>
public sealed class OddsDataService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    ILogger<OddsDataService> logger) : IOddsDataService
{
    private const string BaseUrl = "https://www.football-data.co.uk";

    // League code mapping to Football-Data.co.uk file paths
    private static readonly Dictionary<string, string> LeagueFiles = new()
    {
        { "PL", "E0" },     // Premier League
        { "ELC", "E1" },    // Championship
        { "PD", "SP1" },    // La Liga
        { "BL1", "D1" },    // Bundesliga
        { "SA", "I1" },     // Serie A
        { "FL1", "F1" },    // Ligue 1
        { "DED", "N1" },    // Eredivisie
        { "PPL", "P1" },    // Primeira Liga
    };

    public async Task ImportSeasonOddsAsync(
        string leagueCode,
        string season,
        CancellationToken cancellationToken = default)
    {
        if (!LeagueFiles.TryGetValue(leagueCode, out var fileCode))
        {
            logger.LogWarning("League {Code} not available from Football-Data.co.uk", leagueCode);
            return;
        }

        // Season format: "2425" for 2024/25
        var seasonCode = season.Length == 4
            ? season.Substring(2, 2) + (int.Parse(season.Substring(2, 2)) + 1).ToString("D2")
            : season;

        var url = $"{BaseUrl}/mmz4281/{seasonCode}/{fileCode}.csv";

        var client = httpClientFactory.CreateClient("FootballDataUk");

        try
        {
            var csvContent = await client.GetStringAsync(url, cancellationToken);
            var matchOdds = ParseCsv(csvContent, leagueCode);

            await SaveOddsAsync(matchOdds, cancellationToken);

            logger.LogInformation(
                "Imported {Count} match odds for {League} {Season}",
                matchOdds.Count, leagueCode, season);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Odds file not found for {League} {Season}", leagueCode, season);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import odds for {League} {Season}", leagueCode, season);
        }
    }

    private List<OddsCsvRow> ParseCsv(string csvContent, string leagueCode)
    {
        var results = new List<OddsCsvRow>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2) return results;

        // Parse header
        var headers = lines[0].Split(',');
        var columnMap = headers
            .Select((h, i) => (Header: h.Trim(), Index: i))
            .ToDictionary(x => x.Header, x => x.Index);

        // Required columns
        if (!columnMap.ContainsKey("Date") ||
            !columnMap.ContainsKey("HomeTeam") ||
            !columnMap.ContainsKey("AwayTeam"))
        {
            logger.LogWarning("CSV missing required columns");
            return results;
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length < headers.Length) continue;

            try
            {
                var row = new OddsCsvRow
                {
                    Date = ParseDate(GetValue(values, columnMap, "Date")),
                    HomeTeam = GetValue(values, columnMap, "HomeTeam"),
                    AwayTeam = GetValue(values, columnMap, "AwayTeam"),
                    HomeGoals = ParseIntOrNull(GetValue(values, columnMap, "FTHG")),
                    AwayGoals = ParseIntOrNull(GetValue(values, columnMap, "FTAG")),
                    // Average odds (Bet365 as primary, fallback to others)
                    HomeOdds = ParseDoubleOrNull(GetValue(values, columnMap, "B365H"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "BWH"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "AvgH")),
                    DrawOdds = ParseDoubleOrNull(GetValue(values, columnMap, "B365D"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "BWD"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "AvgD")),
                    AwayOdds = ParseDoubleOrNull(GetValue(values, columnMap, "B365A"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "BWA"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "AvgA")),
                    // Over/Under 2.5
                    Over25 = ParseDoubleOrNull(GetValue(values, columnMap, "B365>2.5"))
                            ?? ParseDoubleOrNull(GetValue(values, columnMap, "Avg>2.5")),
                    Under25 = ParseDoubleOrNull(GetValue(values, columnMap, "B365<2.5"))
                             ?? ParseDoubleOrNull(GetValue(values, columnMap, "Avg<2.5")),

                    // NEW - Match Statistics
                    HomeHalfTimeGoals = ParseIntOrNull(GetValue(values, columnMap, "HTHG")),
                    AwayHalfTimeGoals = ParseIntOrNull(GetValue(values, columnMap, "HTAG")),
                    HomeShots = ParseIntOrNull(GetValue(values, columnMap, "HS")),
                    HomeShotsOnTarget = ParseIntOrNull(GetValue(values, columnMap, "HST")),
                    AwayShots = ParseIntOrNull(GetValue(values, columnMap, "AS")),
                    AwayShotsOnTarget = ParseIntOrNull(GetValue(values, columnMap, "AST")),
                    HomeCorners = ParseIntOrNull(GetValue(values, columnMap, "HC")),
                    AwayCorners = ParseIntOrNull(GetValue(values, columnMap, "AC")),
                    HomeFouls = ParseIntOrNull(GetValue(values, columnMap, "HF")),
                    AwayFouls = ParseIntOrNull(GetValue(values, columnMap, "AF")),
                    HomeYellowCards = ParseIntOrNull(GetValue(values, columnMap, "HY")),
                    AwayYellowCards = ParseIntOrNull(GetValue(values, columnMap, "AY")),
                    HomeRedCards = ParseIntOrNull(GetValue(values, columnMap, "HR")),
                    AwayRedCards = ParseIntOrNull(GetValue(values, columnMap, "AR")),
                    Referee = GetValue(values, columnMap, "Referee") is { Length: > 0 } r ? r : null,
                };

                if (row.Date.HasValue && row.HomeOdds.HasValue)
                {
                    results.Add(row);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to parse CSV row {Index}", i);
            }
        }

        return results;
    }

    private async Task SaveOddsAsync(
        List<OddsCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var row in rows)
        {
            if (!row.Date.HasValue) continue;

            // Find matching match in our database
            var match = await FindMatchAsync(
                row.HomeTeam,
                row.AwayTeam,
                row.Date.Value,
                cancellationToken);

            if (match == null)
            {
                logger.LogDebug(
                    "No match found for {Home} vs {Away} on {Date}",
                    row.HomeTeam, row.AwayTeam, row.Date);
                continue;
            }

            // Save MatchOdds
            var existingOdds = await context.MatchOdds
                .FirstOrDefaultAsync(o => o.MatchId == match.Id, cancellationToken);

            if (existingOdds == null)
            {
                var odds = new MatchOdds
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    HomeWinOdds = row.HomeOdds ?? 0,
                    DrawOdds = row.DrawOdds ?? 0,
                    AwayWinOdds = row.AwayOdds ?? 0,
                    Over25Odds = row.Over25,
                    Under25Odds = row.Under25,
                    ImportedAt = now
                };

                odds.CalculateProbabilities();
                context.MatchOdds.Add(odds);
            }

            // Save MatchStats (NEW)
            var existingStats = await context.MatchStats
                .FirstOrDefaultAsync(s => s.MatchId == match.Id, cancellationToken);

            if (existingStats == null && HasAnyStats(row))
            {
                var stats = new MatchStats
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    HomeShots = row.HomeShots,
                    HomeShotsOnTarget = row.HomeShotsOnTarget,
                    AwayShots = row.AwayShots,
                    AwayShotsOnTarget = row.AwayShotsOnTarget,
                    HomeHalfTimeGoals = row.HomeHalfTimeGoals,
                    AwayHalfTimeGoals = row.AwayHalfTimeGoals,
                    HomeCorners = row.HomeCorners,
                    AwayCorners = row.AwayCorners,
                    HomeFouls = row.HomeFouls,
                    AwayFouls = row.AwayFouls,
                    HomeYellowCards = row.HomeYellowCards,
                    AwayYellowCards = row.AwayYellowCards,
                    HomeRedCards = row.HomeRedCards,
                    AwayRedCards = row.AwayRedCards,
                    Referee = row.Referee,
                    ImportedAt = now
                };

                context.MatchStats.Add(stats);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool HasAnyStats(OddsCsvRow row)
    {
        return row.HomeShots.HasValue || row.AwayShots.HasValue ||
               row.HomeHalfTimeGoals.HasValue || row.HomeCorners.HasValue ||
               row.HomeFouls.HasValue || row.HomeYellowCards.HasValue ||
               row.Referee != null;
    }

    private async Task<Match?> FindMatchAsync(
        string homeTeam,
        string awayTeam,
        DateTime date,
        CancellationToken cancellationToken)
    {
        // Search within +/-1 day to handle timezone differences
        var startDate = date.Date;
        var endDate = date.Date.AddDays(1);

        var matches = await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.MatchDateUtc >= startDate && m.MatchDateUtc < endDate)
            .ToListAsync(cancellationToken);

        var normalizedHome = NormalizeTeamName(homeTeam);
        var normalizedAway = NormalizeTeamName(awayTeam);

        return matches.FirstOrDefault(m =>
            (NormalizeTeamName(m.HomeTeam.Name) == normalizedHome ||
             NormalizeTeamName(m.HomeTeam.ShortName) == normalizedHome) &&
            (NormalizeTeamName(m.AwayTeam.Name) == normalizedAway ||
             NormalizeTeamName(m.AwayTeam.ShortName) == normalizedAway));
    }

    private static string NormalizeTeamName(string name)
    {
        return name.ToLowerInvariant()
            .Replace("fc", "")
            .Replace("cf", "")
            .Replace(" ", "")
            .Trim();
    }

    // CSV parsing helpers
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        foreach (var c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else current.Append(c);
        }
        result.Add(current.ToString().Trim());

        return result.ToArray();
    }

    private static string GetValue(string[] values, Dictionary<string, int> map, string column)
    {
        return map.TryGetValue(column, out var index) && index < values.Length
            ? values[index]
            : "";
    }

    private static DateTime? ParseDate(string value)
    {
        if (DateTime.TryParseExact(value, "dd/MM/yyyy", null, DateTimeStyles.None, out var date))
            return date;
        if (DateTime.TryParseExact(value, "dd/MM/yy", null, DateTimeStyles.None, out date))
            return date;
        return null;
    }

    private static int? ParseIntOrNull(string value)
        => int.TryParse(value, out var result) ? result : null;

    private static double? ParseDoubleOrNull(string value)
        => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;

    public async Task ImportAllLeaguesAsync(CancellationToken cancellationToken = default)
    {
        var currentSeason = GetCurrentSeason();

        foreach (var leagueCode in LeagueFiles.Keys)
        {
            await ImportSeasonOddsAsync(leagueCode, currentSeason, cancellationToken);
            await Task.Delay(1000, cancellationToken); // Rate limiting
        }
    }

    private static string GetCurrentSeason()
    {
        var now = DateTime.UtcNow;
        var year = now.Month >= 8 ? now.Year : now.Year - 1;
        return year.ToString();
    }

    public async Task<MatchOdds?> GetOddsForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        return await context.MatchOdds
            .FirstOrDefaultAsync(o => o.MatchId == matchId, cancellationToken);
    }
}
```

### 3.4 Background Sync Service

**File**: `src/ExtraTime.Infrastructure/Services/ExternalData/OddsSyncBackgroundService.cs`

```csharp
public sealed class OddsSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OddsSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Odds Sync Service started");

        // Initial sync on startup
        await SyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until Monday 5 AM UTC
            var now = DateTime.UtcNow;
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;

            var nextRun = now.Date.AddDays(daysUntilMonday).AddHours(5);
            var delay = nextRun - now;

            logger.LogDebug("Next odds sync at {NextRun}", nextRun);
            await Task.Delay(delay, stoppingToken);

            await SyncAsync(stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOddsDataService>();

            await service.ImportAllLeaguesAsync(cancellationToken);
            logger.LogInformation("Odds sync completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Odds sync failed");
        }
    }
}
```

---

## Part 4: DI Registration

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<IOddsDataService, OddsDataService>();
services.AddHostedService<OddsSyncBackgroundService>();

services.AddHttpClient("FootballDataUk", client =>
{
    client.BaseAddress = new Uri("https://www.football-data.co.uk");
    client.DefaultRequestHeaders.Add("User-Agent", "ExtraTime/1.0");
});
```

---

## Part 5: Configuration

### 5.1 appsettings.json

```json
{
  "ExternalData": {
    "FootballDataUk": {
      "Enabled": true,
      "SyncSchedule": "0 5 * * 1"
    }
  }
}
```

---

## Implementation Checklist

- [ ] Create `MatchOdds` entity
- [ ] Create `MatchOutcome` enum
- [ ] Create `MatchStats` entity (NEW - shots/HT/referee/cards/corners/fouls)
- [ ] Create `MatchOddsConfiguration`
- [ ] Create `MatchStatsConfiguration` (NEW)
- [ ] Create `IOddsDataService` interface
- [ ] Implement `OddsDataService` (CSV parsing with extended fields)
- [ ] Add `ImportHistoricalSeasonsAsync` for multi-season ML backfill (Phase 9.6)
- [ ] Add `GetOddsForMatchAsOfAsync` for leakage-safe historical feature extraction
- [ ] Extend `OddsCsvRow` with match stats fields (HTHG, HTAG, HS, HST, AS, AST, HC, AC, HF, AF, HY, AY, HR, AR, Referee)
- [ ] Update `SaveOddsAsync` to also save `MatchStats`
- [ ] Create `OddsSyncBackgroundService`
- [ ] Add `MatchOdds` and `MatchStats` DbSets to context
- [ ] Add database migration
- [ ] Register services in DI
- [ ] Configure HTTP client
- [ ] Test odds + match stats import

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Entities/MatchOdds.cs` |
| **Create** | `Domain/Entities/MatchStats.cs` |
| **Create** | `Domain/Enums/MatchOutcome.cs` |
| **Create** | `Infrastructure/Data/Configurations/MatchOddsConfiguration.cs` |
| **Create** | `Infrastructure/Data/Configurations/MatchStatsConfiguration.cs` |
| **Create** | `Application/Common/Interfaces/IOddsDataService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/OddsDataService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/OddsSyncBackgroundService.cs` |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **New migration** | `AddMatchOddsAndStats` |
