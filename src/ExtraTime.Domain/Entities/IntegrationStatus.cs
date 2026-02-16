using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

/// <summary>
/// Tracks health and freshness for an external integration.
/// </summary>
public sealed class IntegrationStatus : BaseEntity
{
    public required string IntegrationName { get; set; }

    public IntegrationHealth Health { get; set; } = IntegrationHealth.Unknown;
    public bool IsOperational => Health == IntegrationHealth.Healthy || Health == IntegrationHealth.Degraded;

    public DateTime? LastSuccessfulSync { get; set; }
    public DateTime? LastAttemptedSync { get; set; }
    public DateTime? LastFailedSync { get; set; }

    public int ConsecutiveFailures { get; set; }
    public int TotalFailures24h { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? LastErrorDetails { get; set; }

    public DateTime? DataFreshAsOf { get; set; }
    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromHours(48);
    public bool IsDataStale => DataFreshAsOf.HasValue &&
                               (Clock.UtcNow - DataFreshAsOf.Value) > StaleThreshold;

    public int SuccessfulSyncs24h { get; set; }
    public double SuccessRate24h => (SuccessfulSyncs24h + TotalFailures24h) > 0
        ? (double)SuccessfulSyncs24h / (SuccessfulSyncs24h + TotalFailures24h) * 100
        : 0;
    public TimeSpan? AverageSyncDuration { get; set; }

    public bool IsManuallyDisabled { get; set; }
    public string? DisabledReason { get; set; }
    public DateTime? DisabledAt { get; set; }
    public string? DisabledBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void RecordSuccess(TimeSpan duration)
    {
        var now = Clock.UtcNow;

        LastSuccessfulSync = now;
        LastAttemptedSync = now;
        DataFreshAsOf = now;
        ConsecutiveFailures = 0;
        SuccessfulSyncs24h++;
        Health = IntegrationHealth.Healthy;
        LastErrorMessage = null;
        LastErrorDetails = null;
        AverageSyncDuration = AverageSyncDuration.HasValue
            ? TimeSpan.FromTicks((AverageSyncDuration.Value.Ticks + duration.Ticks) / 2)
            : duration;
        UpdatedAt = now;
    }

    public void RecordFailure(string errorMessage, string? details = null)
    {
        var now = Clock.UtcNow;

        LastAttemptedSync = now;
        LastFailedSync = now;
        ConsecutiveFailures++;
        TotalFailures24h++;
        LastErrorMessage = errorMessage;
        LastErrorDetails = details;
        UpdatedAt = now;

        Health = ConsecutiveFailures switch
        {
            >= 5 => IntegrationHealth.Failed,
            >= 1 => IntegrationHealth.Degraded,
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
    Healthy = 1,
    Degraded = 2,
    Failed = 3,
    Disabled = 4
}
