using System.Net.Http.Json;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class FootballDataService(
    HttpClient httpClient,
    IOptions<FootballDataSettings> settings,
    ILogger<FootballDataService> logger) : IFootballDataService
{
    private readonly FootballDataSettings _settings = settings.Value;

    public async Task<CompetitionApiDto?> GetCompetitionAsync(int externalId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"competitions/{externalId}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CompetitionApiDto>(ct);
        }
        catch (HttpRequestException ex)
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
            var response = await httpClient.GetAsync($"competitions/{competitionExternalId}/teams", ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TeamsApiResponse>(ct);
            return result?.Teams ?? [];
        }
        catch (HttpRequestException ex)
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
            var url = $"competitions/{competitionExternalId}/matches";
            var queryParams = new List<string>();

            if (dateFrom.HasValue)
            {
                queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            }

            if (dateTo.HasValue)
            {
                queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
            }

            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MatchesApiResponse>(ct);
            return result?.Matches ?? [];
        }
        catch (HttpRequestException ex)
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
            var response = await httpClient.GetAsync($"matches?status=IN_PLAY,PAUSED&competitions={competitionIds}", ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MatchesApiResponse>(ct);
            return result?.Matches ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch live matches");
            return [];
        }
    }
}
