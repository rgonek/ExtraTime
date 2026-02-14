using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;

namespace ExtraTime.Infrastructure.Services.Football;

internal sealed class FootballDataService(
    IFootballDataApi footballDataApi,
    IOptions<FootballDataSettings> settings,
    ILogger<FootballDataService> logger) : IFootballDataService
{
    private readonly FootballDataSettings _settings = settings.Value;

    public async Task<CompetitionApiDto?> GetCompetitionAsync(int externalId, CancellationToken ct = default)
    {
        try
        {
            return await footballDataApi.GetCompetitionAsync(externalId, ct);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to fetch competition {ExternalId}", externalId);
            return null;
        }
    }

    public async Task<IReadOnlyList<TeamApiDto>> GetTeamsForCompetitionAsync(
        int competitionExternalId,
        CancellationToken ct = default)
    {
        try
        {
            var result = await footballDataApi.GetTeamsForCompetitionAsync(competitionExternalId, ct);
            return result.Teams;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to fetch teams for competition {ExternalId}", competitionExternalId);
            return [];
        }
    }

    public async Task<IReadOnlyList<MatchApiDto>> GetMatchesForCompetitionAsync(
        int competitionExternalId,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await footballDataApi.GetMatchesForCompetitionAsync(competitionExternalId, dateFrom, dateTo, ct);
            return result.Matches;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to fetch matches for competition {ExternalId}", competitionExternalId);
            return [];
        }
    }

    public async Task<IReadOnlyList<MatchApiDto>> GetLiveMatchesAsync(CancellationToken ct = default)
    {
        try
        {
            var competitionIds = string.Join(",", _settings.SupportedCompetitionIds);
            var result = await footballDataApi.GetMatchesAsync(
                "IN_PLAY,PAUSED,EXTRA_TIME,PENALTY_SHOOTOUT",
                competitionIds,
                ct);
            return result.Matches;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to fetch live matches");
            return [];
        }
    }

    public async Task<StandingsApiResponse?> GetStandingsAsync(int competitionExternalId, CancellationToken ct = default)
    {
        try
        {
            return await footballDataApi.GetStandingsAsync(competitionExternalId, ct);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to fetch standings for competition {ExternalId}", competitionExternalId);
            return null;
        }
    }
}
