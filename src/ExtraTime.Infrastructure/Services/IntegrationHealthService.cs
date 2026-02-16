using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services;

public sealed class IntegrationHealthService(
    IApplicationDbContext context,
    ILogger<IntegrationHealthService> logger) : IIntegrationHealthService
{
    public async Task<IntegrationStatus> GetStatusAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var integrationName = type.ToString();
        var status = await context.IntegrationStatuses
            .FirstOrDefaultAsync(s => s.IntegrationName == integrationName, cancellationToken);

        if (status is not null)
        {
            return status;
        }

        status = new IntegrationStatus
        {
            IntegrationName = integrationName,
            Health = IntegrationHealth.Unknown,
            StaleThreshold = type.GetStaleThreshold(),
            CreatedAt = Clock.UtcNow,
            UpdatedAt = Clock.UtcNow
        };

        context.IntegrationStatuses.Add(status);
        await context.SaveChangesAsync(cancellationToken);
        return status;
    }

    public async Task<List<IntegrationStatus>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var type in Enum.GetValues<IntegrationType>())
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
            "Integration {IntegrationType} sync successful ({Duration:g})",
            type,
            duration);
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
            "Integration {IntegrationType} sync failed ({FailureCount} consecutive): {ErrorMessage}",
            type,
            status.ConsecutiveFailures,
            errorMessage);
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
        status.DisabledAt = Clock.UtcNow;
        status.Health = IntegrationHealth.Disabled;
        status.UpdatedAt = Clock.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Integration {IntegrationType} manually disabled by {DisabledBy}: {Reason}",
            type,
            disabledBy,
            reason);
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
        status.Health = IntegrationHealth.Unknown;
        status.UpdatedAt = Clock.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Integration {IntegrationType} re-enabled", type);
    }

    public async Task<DataAvailability> GetDataAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await GetAllStatusesAsync(cancellationToken);

        var statusByName = statuses.ToDictionary(s => s.IntegrationName, StringComparer.Ordinal);

        var xgStatus = statusByName.GetValueOrDefault(IntegrationType.Understat.ToString());
        var oddsStatus = statusByName.GetValueOrDefault(IntegrationType.FootballDataUk.ToString());
        var injuriesStatus = statusByName.GetValueOrDefault(IntegrationType.ApiFootball.ToString());
        var eloStatus = statusByName.GetValueOrDefault(IntegrationType.ClubElo.ToString());
        var footballDataStatus = statusByName.GetValueOrDefault(IntegrationType.FootballDataOrg.ToString());

        return new DataAvailability
        {
            XgDataAvailable = IsFreshAndOperational(xgStatus),
            OddsDataAvailable = IsFreshAndOperational(oddsStatus),
            InjuryDataAvailable = IsOperational(injuriesStatus),
            LineupDataAvailable = IsOperational(footballDataStatus),
            EloDataAvailable = IsFreshAndOperational(eloStatus),
            StandingsDataAvailable = IsOperational(footballDataStatus)
        };
    }

    private static bool IsOperational(IntegrationStatus? status)
    {
        return status is { IsManuallyDisabled: false } && status.IsOperational;
    }

    private static bool IsFreshAndOperational(IntegrationStatus? status)
    {
        return IsOperational(status) && !status!.IsDataStale;
    }
}
