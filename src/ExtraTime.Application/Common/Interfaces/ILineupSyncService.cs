namespace ExtraTime.Application.Common.Interfaces;

public interface ILineupSyncService
{
    Task<bool> SyncLineupForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<int> SyncLineupsForUpcomingMatchesAsync(
        TimeSpan lookAhead,
        CancellationToken cancellationToken = default);
}
