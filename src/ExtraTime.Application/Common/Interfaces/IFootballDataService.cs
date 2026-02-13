using ExtraTime.Application.Features.Football.DTOs;

namespace ExtraTime.Application.Common.Interfaces;

public interface IFootballDataService
{
    Task<CompetitionApiDto?> GetCompetitionAsync(int externalId, CancellationToken ct = default);
    Task<IReadOnlyList<TeamApiDto>> GetTeamsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);
    Task<IReadOnlyList<MatchApiDto>> GetMatchesForCompetitionAsync(int competitionExternalId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
    Task<IReadOnlyList<MatchApiDto>> GetLiveMatchesAsync(CancellationToken ct = default);
    Task<StandingsApiResponse?> GetStandingsAsync(int competitionExternalId, CancellationToken ct = default);
}
