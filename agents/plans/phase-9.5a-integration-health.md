# Phase 9.5A: Integration Health & Monitoring

## Overview
Track the status of each external data source so bots can gracefully degrade when data is unavailable. This is the **foundation** for all other Phase 9.5 integrations.

> **Prerequisite**: Phase 9 (Extended Football Data) should be complete
> **Priority**: Required first - all other integrations depend on this

---

## Part 1: Domain Layer

### 1.1 IntegrationStatus Entity

**File**: `src/ExtraTime.Domain/Entities/IntegrationStatus.cs`

```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// Tracks the health status of external data integrations.
/// Used by bots to know which data sources are reliable.
/// </summary>
public sealed class IntegrationStatus : BaseEntity
{
    public required string IntegrationName { get; set; }  // e.g., "Understat", "FootballDataUk"

    // Current status
    public IntegrationHealth Health { get; set; } = IntegrationHealth.Unknown;
    public bool IsOperational => Health == IntegrationHealth.Healthy || Health == IntegrationHealth.Degraded;

    // Last sync info
    public DateTime? LastSuccessfulSync { get; set; }
    public DateTime? LastAttemptedSync { get; set; }
    public DateTime? LastFailedSync { get; set; }

    // Failure tracking
    public int ConsecutiveFailures { get; set; }
    public int TotalFailures24h { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? LastErrorDetails { get; set; }  // Stack trace or details

    // Data freshness
    public DateTime? DataFreshAsOf { get; set; }     // When the data was last updated
    public bool IsDataStale => DataFreshAsOf.HasValue &&
        (DateTime.UtcNow - DataFreshAsOf.Value) > StaleThreshold;
    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromHours(48);

    // Statistics
    public int SuccessfulSyncs24h { get; set; }
    public double SuccessRate24h => (SuccessfulSyncs24h + TotalFailures24h) > 0
        ? (double)SuccessfulSyncs24h / (SuccessfulSyncs24h + TotalFailures24h) * 100
        : 0;
    public TimeSpan? AverageSyncDuration { get; set; }

    // Manual override
    public bool IsManuallyDisabled { get; set; }
    public string? DisabledReason { get; set; }
    public DateTime? DisabledAt { get; set; }
    public string? DisabledBy { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Methods
    public void RecordSuccess(TimeSpan duration)
    {
        LastSuccessfulSync = DateTime.UtcNow;
        LastAttemptedSync = DateTime.UtcNow;
        DataFreshAsOf = DateTime.UtcNow;
        ConsecutiveFailures = 0;
        SuccessfulSyncs24h++;
        Health = IntegrationHealth.Healthy;
        LastErrorMessage = null;
        LastErrorDetails = null;
        AverageSyncDuration = AverageSyncDuration.HasValue
            ? TimeSpan.FromTicks((AverageSyncDuration.Value.Ticks + duration.Ticks) / 2)
            : duration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailure(string errorMessage, string? details = null)
    {
        LastAttemptedSync = DateTime.UtcNow;
        LastFailedSync = DateTime.UtcNow;
        ConsecutiveFailures++;
        TotalFailures24h++;
        LastErrorMessage = errorMessage;
        LastErrorDetails = details;
        UpdatedAt = DateTime.UtcNow;

        // Update health based on failure count
        Health = ConsecutiveFailures switch
        {
            1 => IntegrationHealth.Degraded,
            >= 2 and < 5 => IntegrationHealth.Degraded,
            >= 5 => IntegrationHealth.Failed,
            _ => IntegrationHealth.Unknown
        };
    }

    public void ResetDailyCounters()
    {
        SuccessfulSyncs24h = 0;
        TotalFailures24h = 0;
    }
}

public enum IntegrationHealth
{
    Unknown = 0,
    Healthy = 1,      // Working normally
    Degraded = 2,     // Some issues but data available
    Failed = 3,       // Not working, data may be stale
    Disabled = 4      // Manually disabled
}
```

### 1.2 IntegrationType Enum

**File**: `src/ExtraTime.Domain/Enums/IntegrationType.cs`

```csharp
namespace ExtraTime.Domain.Enums;

public enum IntegrationType
{
    FootballDataOrg = 0,    // Primary match data
    Understat = 1,          // xG statistics
    FootballDataUk = 2,     // Betting odds
    ApiFootball = 3,        // Injuries
    ClubElo = 4             // Elo ratings
}

public static class IntegrationTypeExtensions
{
    public static string ToName(this IntegrationType type) => type switch
    {
        IntegrationType.FootballDataOrg => "Football-Data.org",
        IntegrationType.Understat => "Understat",
        IntegrationType.FootballDataUk => "Football-Data.co.uk",
        IntegrationType.ApiFootball => "API-Football",
        IntegrationType.ClubElo => "ClubElo.com",
        _ => type.ToString()
    };

    public static TimeSpan GetStaleThreshold(this IntegrationType type) => type switch
    {
        IntegrationType.FootballDataOrg => TimeSpan.FromHours(6),
        IntegrationType.Understat => TimeSpan.FromHours(48),
        IntegrationType.FootballDataUk => TimeSpan.FromDays(7),
        IntegrationType.ApiFootball => TimeSpan.FromHours(48),
        IntegrationType.ClubElo => TimeSpan.FromHours(48),
        _ => TimeSpan.FromHours(24)
    };
}
```

---

## Part 2: Application Layer

### 2.1 IIntegrationHealthService Interface

**File**: `src/ExtraTime.Application/Common/Interfaces/IIntegrationHealthService.cs`

```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IIntegrationHealthService
{
    Task<IntegrationStatus> GetStatusAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default);

    Task<List<IntegrationStatus>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default);

    Task RecordSuccessAsync(
        IntegrationType type,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    Task RecordFailureAsync(
        IntegrationType type,
        string errorMessage,
        string? details = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsOperationalAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default);

    Task<bool> HasFreshDataAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default);

    Task DisableIntegrationAsync(
        IntegrationType type,
        string reason,
        string disabledBy,
        CancellationToken cancellationToken = default);

    Task EnableIntegrationAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default);

    Task<DataAvailability> GetDataAvailabilityAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary of which data sources are available for bot predictions.
/// </summary>
public sealed record DataAvailability
{
    public bool FormDataAvailable { get; init; } = true;  // Always available (calculated)
    public bool XgDataAvailable { get; init; }
    public bool OddsDataAvailable { get; init; }
    public bool InjuryDataAvailable { get; init; }
    public bool LineupDataAvailable { get; init; }
    public bool StandingsDataAvailable { get; init; }

    public bool HasAnyExternalData => XgDataAvailable || OddsDataAvailable ||
                                       InjuryDataAvailable || LineupDataAvailable;

    public int AvailableSourceCount => new[]
    {
        FormDataAvailable, XgDataAvailable, OddsDataAvailable,
        InjuryDataAvailable, LineupDataAvailable, StandingsDataAvailable
    }.Count(x => x);
}
```

---

## Part 3: Infrastructure Layer

### 3.1 IntegrationHealthService Implementation

**File**: `src/ExtraTime.Infrastructure/Services/IntegrationHealthService.cs`

```csharp
namespace ExtraTime.Infrastructure.Services;

public sealed class IntegrationHealthService(
    IApplicationDbContext context,
    ILogger<IntegrationHealthService> logger) : IIntegrationHealthService
{
    public async Task<IntegrationStatus> GetStatusAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var name = type.ToString();
        var status = await context.IntegrationStatuses
            .FirstOrDefaultAsync(s => s.IntegrationName == name, cancellationToken);

        if (status == null)
        {
            status = new IntegrationStatus
            {
                Id = Guid.NewGuid(),
                IntegrationName = name,
                Health = IntegrationHealth.Unknown,
                StaleThreshold = type.GetStaleThreshold(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.IntegrationStatuses.Add(status);
            await context.SaveChangesAsync(cancellationToken);
        }

        return status;
    }

    public async Task<List<IntegrationStatus>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        // Ensure all integrations exist
        foreach (IntegrationType type in Enum.GetValues<IntegrationType>())
        {
            await GetStatusAsync(type, cancellationToken);
        }

        return await context.IntegrationStatuses
            .OrderBy(s => s.IntegrationName)
            .ToListAsync(cancellationToken);
    }

    public async Task RecordSuccessAsync(
        IntegrationType type,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        status.RecordSuccess(duration);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Integration {Type} sync successful ({Duration:g})",
            type, duration);
    }

    public async Task RecordFailureAsync(
        IntegrationType type,
        string errorMessage,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        status.RecordFailure(errorMessage, details);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Integration {Type} sync failed ({Failures} consecutive): {Error}",
            type, status.ConsecutiveFailures, errorMessage);
    }

    public async Task<bool> IsOperationalAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        return status.IsOperational && !status.IsManuallyDisabled;
    }

    public async Task<bool> HasFreshDataAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        return status.IsOperational && !status.IsDataStale && !status.IsManuallyDisabled;
    }

    public async Task<DataAvailability> GetDataAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await GetAllStatusesAsync(cancellationToken);

        bool IsAvailable(IntegrationType type) =>
            statuses.FirstOrDefault(s => s.IntegrationName == type.ToString())
                ?.IsOperational ?? false;

        bool HasFresh(IntegrationType type) =>
            statuses.FirstOrDefault(s => s.IntegrationName == type.ToString()) is { } s
            && s.IsOperational && !s.IsDataStale;

        return new DataAvailability
        {
            XgDataAvailable = HasFresh(IntegrationType.Understat),
            OddsDataAvailable = HasFresh(IntegrationType.FootballDataUk),
            InjuryDataAvailable = IsAvailable(IntegrationType.ApiFootball),
            LineupDataAvailable = IsAvailable(IntegrationType.FootballDataOrg),
            StandingsDataAvailable = IsAvailable(IntegrationType.FootballDataOrg)
        };
    }

    public async Task DisableIntegrationAsync(
        IntegrationType type,
        string reason,
        string disabledBy,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        status.IsManuallyDisabled = true;
        status.DisabledReason = reason;
        status.DisabledBy = disabledBy;
        status.DisabledAt = DateTime.UtcNow;
        status.Health = IntegrationHealth.Disabled;
        status.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Integration {Type} manually disabled by {User}: {Reason}",
            type, disabledBy, reason);
    }

    public async Task EnableIntegrationAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        status.IsManuallyDisabled = false;
        status.DisabledReason = null;
        status.DisabledBy = null;
        status.DisabledAt = null;
        status.Health = IntegrationHealth.Unknown; // Will update on next sync
        status.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Integration {Type} re-enabled", type);
    }
}
```

### 3.2 Health Tracking Wrapper Example

Each sync service should wrap its sync logic with health tracking:

```csharp
public async Task SyncAllLeaguesAsync(CancellationToken cancellationToken = default)
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        var currentSeason = GetCurrentSeason();

        foreach (var leagueCode in LeagueMapping.Keys)
        {
            await SyncLeagueXgStatsAsync(leagueCode, currentSeason, cancellationToken);
            await Task.Delay(2000, cancellationToken);
        }

        stopwatch.Stop();
        await _healthService.RecordSuccessAsync(
            IntegrationType.Understat,
            stopwatch.Elapsed,
            cancellationToken);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        await _healthService.RecordFailureAsync(
            IntegrationType.Understat,
            ex.Message,
            ex.StackTrace,
            cancellationToken);
        throw;
    }
}
```

---

## Implementation Checklist

- [x] Create `IntegrationStatus` entity
- [x] Create `IntegrationHealth` enum
- [x] Create `IntegrationType` enum (with `ClubElo = 4`)
- [x] Create `IntegrationTypeExtensions` (with ClubElo mapping)
- [x] Create `IntegrationStatusConfiguration`
- [x] Create `IIntegrationHealthService` interface
- [x] Create `DataAvailability` record
- [x] Implement `IntegrationHealthService`
- [x] Add `IntegrationStatuses` DbSet to context
- [x] Create database migration
- [x] Register services in DI

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Entities/IntegrationStatus.cs` |
| **Create** | `Domain/Enums/IntegrationType.cs` |
| **Create** | `Application/Common/Interfaces/IIntegrationHealthService.cs` |
| **Create** | `Infrastructure/Services/IntegrationHealthService.cs` |
| **Create** | `Infrastructure/Data/Configurations/IntegrationStatusConfiguration.cs` |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **New migration** | `AddIntegrationStatuses` |
