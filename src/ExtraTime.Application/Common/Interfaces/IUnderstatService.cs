using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface IUnderstatService
{
    Task<List<TeamXgStats>> SyncLeagueXgStatsAsync(
        string leagueCode,
        string season,
        DateTime? snapshotDateUtc = null,
        CancellationToken cancellationToken = default);

    Task SyncLeagueSeasonRangeAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        CancellationToken cancellationToken = default);

    Task<TeamXgStats?> GetTeamXgAsync(
        Guid teamId,
        Guid competitionId,
        string season,
        CancellationToken cancellationToken = default);

    Task<TeamXgStats?> GetTeamXgAsOfAsync(
        Guid teamId,
        Guid competitionId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    Task<MatchXgStats?> GetMatchXgAsync(
        int understatMatchId,
        CancellationToken cancellationToken = default);

    Task SyncAllLeaguesAsync(CancellationToken cancellationToken = default);
}
