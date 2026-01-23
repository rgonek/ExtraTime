namespace ExtraTime.Application.Common.Interfaces;

public interface IFootballSyncService
{
    Task SyncCompetitionsAsync(CancellationToken ct = default);
    Task SyncTeamsForCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task SyncMatchesAsync(DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
    Task SyncLiveMatchResultsAsync(CancellationToken ct = default);
}
