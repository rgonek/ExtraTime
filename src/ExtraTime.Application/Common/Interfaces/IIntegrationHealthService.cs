using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

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
/// Summary of available data sources for bot strategies.
/// </summary>
public sealed record DataAvailability
{
    public bool FormDataAvailable { get; init; } = true;
    public bool XgDataAvailable { get; init; }
    public bool OddsDataAvailable { get; init; }
    public bool InjuryDataAvailable { get; init; }
    public bool LineupDataAvailable { get; init; }
    public bool EloDataAvailable { get; init; }
    public bool StandingsDataAvailable { get; init; }

    public bool HasAnyExternalData => XgDataAvailable || OddsDataAvailable ||
                                       InjuryDataAvailable || LineupDataAvailable || EloDataAvailable;

    public int AvailableSourceCount => new[]
    {
        FormDataAvailable, XgDataAvailable, OddsDataAvailable,
        InjuryDataAvailable, LineupDataAvailable, EloDataAvailable, StandingsDataAvailable
    }.Count(x => x);
}
