using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface IOddsDataService
{
    Task ImportSeasonOddsAsync(
        string leagueCode,
        string season,
        DateTime? importedAtUtc = null,
        CancellationToken cancellationToken = default);

    Task ImportAllLeaguesAsync(CancellationToken cancellationToken = default);

    Task ImportHistoricalSeasonsAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        DateTime? importedAtUtc = null,
        CancellationToken cancellationToken = default);

    Task<MatchOdds?> GetOddsForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<MatchOdds?> GetOddsForMatchAsOfAsync(
        Guid matchId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}
