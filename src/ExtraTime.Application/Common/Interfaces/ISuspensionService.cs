using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface ISuspensionService
{
    Task SyncSuspensionsForUpcomingMatchesAsync(
        int daysAhead = 3,
        CancellationToken cancellationToken = default);

    Task<TeamSuspensions?> GetTeamSuspensionsAsOfAsync(
        Guid teamId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}
