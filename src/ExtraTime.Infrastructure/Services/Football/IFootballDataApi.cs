using ExtraTime.Application.Features.Football.DTOs;
using Refit;

namespace ExtraTime.Infrastructure.Services.Football;

internal interface IFootballDataApi
{
    [Get("/competitions/{externalId}")]
    Task<CompetitionApiDto> GetCompetitionAsync(int externalId, CancellationToken ct = default);

    [Get("/competitions/{competitionExternalId}/teams")]
    Task<TeamsApiResponse> GetTeamsForCompetitionAsync(
        int competitionExternalId,
        [Query] int? season = null,
        CancellationToken ct = default);

    [Get("/competitions/{competitionExternalId}/matches")]
    Task<MatchesApiResponse> GetMatchesForCompetitionAsync(
        int competitionExternalId,
        [Query] int? season = null,
        [Query] int? matchday = null,
        [Query] string? status = null,
        [Query(Format = "yyyy-MM-dd")] DateTime? dateFrom = null,
        [Query(Format = "yyyy-MM-dd")] DateTime? dateTo = null,
        [Query] string? stage = null,
        [Query] string? group = null,
        CancellationToken ct = default);

    [Get("/matches")]
    Task<MatchesApiResponse> GetMatchesAsync(
        [Query] string status,
        [Query] string competitions,
        CancellationToken ct = default);

    [Get("/competitions/{competitionExternalId}/standings")]
    Task<StandingsApiResponse> GetStandingsAsync(
        int competitionExternalId,
        [Query] int? season = null,
        [Query] int? matchday = null,
        [Query(Format = "yyyy-MM-dd")] DateTime? date = null,
        CancellationToken ct = default);
}
