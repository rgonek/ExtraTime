using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface IInjuryService
{
    /// <summary>
    /// Sync injuries for upcoming fixtures while honoring lineups-first shared quota rules.
    /// </summary>
    Task SyncInjuriesForUpcomingMatchesAsync(
        int daysAhead = 3,
        CancellationToken cancellationToken = default);

    Task<TeamInjuries?> GetTeamInjuriesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Leakage-safe as-of read for training. Returns null when no historical snapshot exists.
    /// </summary>
    Task<TeamInjuries?> GetTeamInjuriesAsOfAsync(
        Guid teamId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    double CalculateInjuryImpact(TeamInjuries injuries);
}
