namespace ExtraTime.Application.Common.Interfaces;

public interface IFootballSyncService
{
    Task SyncCompetitionsAsync(CancellationToken ct = default);
    Task SyncTeamsForCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task SyncMatchesAsync(DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
    Task SyncLiveMatchResultsAsync(CancellationToken ct = default);

    Task<MatchSyncResult> SyncMatchesForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);
    Task<StandingsSyncResult> SyncStandingsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);
    Task SyncTeamsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);
}

public sealed record MatchSyncResult(int CompetitionExternalId, bool HasNewlyFinishedMatches);

public sealed record StandingsSyncResult(int CompetitionExternalId, bool NewSeasonDetected);
