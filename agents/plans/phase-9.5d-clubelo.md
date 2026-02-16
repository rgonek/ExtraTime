# Phase 9.5D: ClubElo.com Integration - Elo Ratings

## Overview
Integrate Elo ratings from ClubElo.com to provide team quality indicators for ML prediction and bot strategy enhancement.

> **Data Source**: `http://api.clubelo.com/{YYYY-MM-DD}` returns CSV with columns: `Rank,Club,Country,Level,Elo,From,To`
> **Sync Strategy**: Daily at 3 AM UTC. Single HTTP request returns all teams.
> **Rate Limit**: None (public CSV)

> **Prerequisite**: Phase 9.5A (Integration Health) must be complete

---

## Part 1: Domain Layer

### 1.1 TeamEloRating Entity

**File**: `src/ExtraTime.Domain/Entities/TeamEloRating.cs`

```csharp
namespace ExtraTime.Domain.Entities;

public sealed class TeamEloRating : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public double EloRating { get; set; }         // e.g., 1843.5
    public int EloRank { get; set; }              // Global rank
    public string ClubEloName { get; set; } = ""; // Name in ClubElo system

    public DateTime RatingDate { get; set; }      // Date this rating was for
    public DateTime SyncedAt { get; set; }
}
```

---

## Part 2: Infrastructure Layer

### 2.1 EF Configuration

**File**: `src/ExtraTime.Infrastructure/Data/Configurations/TeamEloRatingConfiguration.cs`

```csharp
public sealed class TeamEloRatingConfiguration : IEntityTypeConfiguration<TeamEloRating>
{
    public void Configure(EntityTypeBuilder<TeamEloRating> builder)
    {
        builder.ToTable("TeamEloRatings");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ClubEloName).HasMaxLength(100);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TeamId, t.RatingDate })
            .IsUnique();
    }
}
```

### 2.2 ApplicationDbContext

Add to `ApplicationDbContext.cs`:
```csharp
public DbSet<TeamEloRating> TeamEloRatings => Set<TeamEloRating>();
```

Add to `IApplicationDbContext.cs`:
```csharp
DbSet<TeamEloRating> TeamEloRatings { get; }
```

---

## Part 3: Service Layer

### 3.1 Interface

**File**: `src/ExtraTime.Application/Common/Interfaces/IEloRatingService.cs`

```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IEloRatingService
{
    Task SyncEloRatingsAsync(CancellationToken cancellationToken = default);
    Task<TeamEloRating?> GetTeamEloAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamEloRating?> GetTeamEloAtDateAsync(Guid teamId, DateTime date, CancellationToken cancellationToken = default);
}
```

### 3.2 Implementation

**File**: `src/ExtraTime.Infrastructure/Services/ExternalData/EloRatingService.cs`

```csharp
namespace ExtraTime.Infrastructure.Services.ExternalData;

public sealed class EloRatingService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    IIntegrationHealthService healthService,
    ILogger<EloRatingService> logger) : IEloRatingService
{
    private const string BaseUrl = "http://api.clubelo.com";

    // Map ClubElo team names to our team names (common mismatches)
    private static readonly Dictionary<string, string> TeamNameMapping = new()
    {
        { "Man City", "Manchester City" },
        { "Man United", "Manchester United" },
        { "Spurs", "Tottenham Hotspur" },
        { "Wolves", "Wolverhampton Wanderers" },
        { "West Ham", "West Ham United" },
        { "Sheffield Utd", "Sheffield United" },
        { "Nott'm Forest", "Nottingham Forest" },
        { "Newcastle", "Newcastle United" },
        { "Leicester", "Leicester City" },
        { "Leeds", "Leeds United" },
        { "Brighton", "Brighton and Hove Albion" },
        { "Atletico Madrid", "Atletico Madrid" },
        { "Athletic Bilbao", "Athletic Club" },
        { "Inter", "Inter Milan" },
        { "AC Milan", "Milan" },
        { "PSG", "Paris Saint-Germain" },
    };

    public async Task SyncEloRatingsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var today = DateTime.UtcNow.Date;
        var dateStr = today.ToString("yyyy-MM-dd");

        try
        {
            var client = httpClientFactory.CreateClient("ClubElo");
            var csvContent = await client.GetStringAsync($"{BaseUrl}/{dateStr}", cancellationToken);

            var ratings = ParseEloCsv(csvContent, today);
            await SaveEloRatingsAsync(ratings, cancellationToken);

            stopwatch.Stop();
            await healthService.RecordSuccessAsync(
                IntegrationType.ClubElo,
                stopwatch.Elapsed,
                cancellationToken);

            logger.LogInformation("Synced {Count} Elo ratings for {Date}", ratings.Count, dateStr);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await healthService.RecordFailureAsync(
                IntegrationType.ClubElo,
                ex.Message,
                ex.StackTrace,
                cancellationToken);
            throw;
        }
    }

    private List<EloRatingRow> ParseEloCsv(string csvContent, DateTime ratingDate)
    {
        var results = new List<EloRatingRow>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip header: Rank,Club,Country,Level,Elo,From,To
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 5) continue;

            if (int.TryParse(parts[0], out var rank) &&
                double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var elo))
            {
                results.Add(new EloRatingRow
                {
                    Rank = rank,
                    ClubName = parts[1].Trim(),
                    Country = parts[2].Trim(),
                    Level = int.TryParse(parts[3], out var level) ? level : 1,
                    EloRating = elo,
                    RatingDate = ratingDate
                });
            }
        }

        return results;
    }

    private async Task SaveEloRatingsAsync(
        List<EloRatingRow> ratings,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var rating in ratings)
        {
            // Skip non-top-flight teams
            if (rating.Level > 1) continue;

            var teamName = TeamNameMapping.TryGetValue(rating.ClubName, out var mapped)
                ? mapped : rating.ClubName;

            var team = await context.Teams
                .FirstOrDefaultAsync(t =>
                    t.Name == teamName ||
                    t.ShortName == teamName ||
                    t.Name == rating.ClubName,
                    cancellationToken);

            if (team == null) continue;

            var existing = await context.TeamEloRatings
                .FirstOrDefaultAsync(e =>
                    e.TeamId == team.Id &&
                    e.RatingDate == rating.RatingDate,
                    cancellationToken);

            if (existing != null)
            {
                existing.EloRating = rating.EloRating;
                existing.EloRank = rating.Rank;
                existing.SyncedAt = now;
            }
            else
            {
                context.TeamEloRatings.Add(new TeamEloRating
                {
                    Id = Guid.NewGuid(),
                    TeamId = team.Id,
                    EloRating = rating.EloRating,
                    EloRank = rating.Rank,
                    ClubEloName = rating.ClubName,
                    RatingDate = rating.RatingDate,
                    SyncedAt = now
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TeamEloRating?> GetTeamEloAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamEloRatings
            .Where(e => e.TeamId == teamId)
            .OrderByDescending(e => e.RatingDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TeamEloRating?> GetTeamEloAtDateAsync(
        Guid teamId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamEloRatings
            .Where(e => e.TeamId == teamId && e.RatingDate <= date)
            .OrderByDescending(e => e.RatingDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

internal sealed class EloRatingRow
{
    public int Rank { get; set; }
    public string ClubName { get; set; } = "";
    public string Country { get; set; } = "";
    public int Level { get; set; }
    public double EloRating { get; set; }
    public DateTime RatingDate { get; set; }
}
```

### 3.3 Background Sync Service

**File**: `src/ExtraTime.Infrastructure/Services/ExternalData/EloSyncBackgroundService.cs`

```csharp
public sealed class EloSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EloSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Elo Rating Sync Service started");

        // Initial sync on startup
        await SyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until 3 AM UTC
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(3);
            var delay = nextRun - now;

            logger.LogDebug("Next Elo sync at {NextRun}", nextRun);
            await Task.Delay(delay, stoppingToken);

            await SyncAsync(stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEloRatingService>();

            await service.SyncEloRatingsAsync(cancellationToken);
            logger.LogInformation("Elo rating sync completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Elo rating sync failed");
        }
    }
}
```

---

## Part 4: DI Registration

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<IEloRatingService, EloRatingService>();
services.AddHostedService<EloSyncBackgroundService>();

services.AddHttpClient("ClubElo", client =>
{
    client.BaseAddress = new Uri("http://api.clubelo.com");
    client.DefaultRequestHeaders.Add("User-Agent", "ExtraTime/1.0");
});
```

---

## Part 5: Configuration

### 5.1 appsettings.json

```json
{
  "ExternalData": {
    "ClubElo": {
      "Enabled": true,
      "SyncSchedule": "0 3 * * *"
    }
  }
}
```

---

## Implementation Checklist

- [ ] Create `TeamEloRating` entity
- [ ] Create `TeamEloRatingConfiguration`
- [ ] Add `TeamEloRatings` DbSet to context
- [ ] Create `IEloRatingService` interface
- [ ] Implement `EloRatingService` (CSV parse from clubelo.com)
- [ ] Create `EloSyncBackgroundService`
- [ ] Add `ClubElo` to `IntegrationType` enum
- [ ] Add team name mapping for ClubElo names
- [ ] Register services in DI
- [ ] Configure HTTP client
- [ ] Add database migration
- [ ] Test Elo sync

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Entities/TeamEloRating.cs` |
| **Create** | `Infrastructure/Data/Configurations/TeamEloRatingConfiguration.cs` |
| **Create** | `Application/Common/Interfaces/IEloRatingService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/EloRatingService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/EloSyncBackgroundService.cs` |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **Modify** | `Domain/Enums/IntegrationType.cs` (add ClubElo = 4) |
| **New migration** | `AddTeamEloRatings` |
