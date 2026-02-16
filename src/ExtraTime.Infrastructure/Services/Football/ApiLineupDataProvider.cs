using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class ApiLineupDataProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ApiFootballSettings> settings,
    ILogger<ApiLineupDataProvider> logger) : ILineupDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<MatchLineupData?> GetMatchLineupAsync(
        MatchLineupRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiSettings = settings.Value;
        if (!apiSettings.Enabled || string.IsNullOrWhiteSpace(apiSettings.ApiKey))
        {
            logger.LogDebug(
                "API lineup request skipped for match {ExternalId} because API-Football is disabled or missing API key.",
                request.MatchExternalId);
            return null;
        }

        var client = httpClientFactory.CreateClient("ApiFootball");
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/v3/fixtures/lineups?fixture={request.MatchExternalId}");
        httpRequest.Headers.TryAddWithoutValidation("X-RapidAPI-Key", apiSettings.ApiKey);

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "API-Football lineup request failed for fixture {FixtureId} with status {StatusCode}.",
                request.MatchExternalId,
                response.StatusCode);
            return null;
        }

        ApiFootballLineupResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<ApiFootballLineupResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed parsing lineup response for fixture {FixtureId}.", request.MatchExternalId);
            return null;
        }

        if (payload?.Response is null || payload.Response.Count < 2)
        {
            return null;
        }

        var homeSource = ResolveTeamLineup(payload.Response, request.HomeTeamName, 0);
        var awaySource = ResolveTeamLineup(payload.Response, request.AwayTeamName, 1);
        if (homeSource is null || awaySource is null)
        {
            logger.LogDebug(
                "Could not resolve home/away lineup mapping for fixture {FixtureId}.",
                request.MatchExternalId);
            return null;
        }

        return new MatchLineupData(
            MapTeamLineup(homeSource),
            MapTeamLineup(awaySource));
    }

    private static ApiFootballTeamLineup? ResolveTeamLineup(
        IReadOnlyList<ApiFootballTeamLineup> lineups,
        string expectedTeamName,
        int fallbackIndex)
    {
        var normalizedExpected = NormalizeName(expectedTeamName);
        var match = lineups.FirstOrDefault(x => NormalizeName(x.Team?.Name) == normalizedExpected);
        if (match is not null)
        {
            return match;
        }

        return lineups.Count > fallbackIndex ? lineups[fallbackIndex] : null;
    }

    private static TeamLineupData MapTeamLineup(ApiFootballTeamLineup source)
    {
        var startingXi = source.StartXI?
            .Select(x => x.Player)
            .Where(x => x is not null)
            .Select(x => MapPlayer(x!))
            .ToList() ?? [];
        var bench = source.Substitutes?
            .Select(x => x.Player)
            .Where(x => x is not null)
            .Select(x => MapPlayer(x!))
            .ToList() ?? [];

        var captainName = source.StartXI?
            .Select(x => x.Player)
            .FirstOrDefault(x => x is not null && x.Captain)?
            .Name;

        return new TeamLineupData(
            Formation: string.IsNullOrWhiteSpace(source.Formation) ? null : source.Formation.Trim(),
            CoachName: source.Coach?.Name is { Length: > 0 } coachName ? coachName.Trim() : null,
            CaptainName: string.IsNullOrWhiteSpace(captainName) ? null : captainName.Trim(),
            StartingXi: startingXi,
            Bench: bench);
    }

    private static LineupPlayerData MapPlayer(ApiFootballLineupPlayer source)
    {
        return new LineupPlayerData(
            source.Id,
            NormalizeName(source.Name),
            NormalizePosition(source.Position, source.Grid),
            source.Number);
    }

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NormalizePosition(string? position, string? grid)
    {
        if (!string.IsNullOrWhiteSpace(position))
        {
            var normalized = position.Trim().ToUpperInvariant();
            return normalized switch
            {
                "G" or "GK" or "GOALKEEPER" => "GK",
                "D" or "DEF" or "DEFENDER" => "DEF",
                "M" or "MID" or "MIDFIELDER" => "MID",
                "F" or "FWD" or "ATT" or "ATTACKER" => "FWD",
                _ => normalized
            };
        }

        if (string.IsNullOrWhiteSpace(grid))
        {
            return null;
        }

        var rowText = grid.Split(':', 2)[0];
        if (!int.TryParse(rowText, out var row))
        {
            return null;
        }

        return row switch
        {
            1 => "GK",
            2 => "DEF",
            3 => "MID",
            4 => "FWD",
            _ => null
        };
    }

    internal sealed class ApiFootballLineupResponse
    {
        [JsonPropertyName("response")]
        public List<ApiFootballTeamLineup>? Response { get; set; }
    }

    internal sealed class ApiFootballTeamLineup
    {
        [JsonPropertyName("team")]
        public ApiFootballTeam? Team { get; set; }

        [JsonPropertyName("coach")]
        public ApiFootballCoach? Coach { get; set; }

        [JsonPropertyName("formation")]
        public string? Formation { get; set; }

        [JsonPropertyName("startXI")]
        public List<ApiFootballLineupPlayerWrapper>? StartXI { get; set; }

        [JsonPropertyName("substitutes")]
        public List<ApiFootballLineupPlayerWrapper>? Substitutes { get; set; }
    }

    internal sealed class ApiFootballLineupPlayerWrapper
    {
        [JsonPropertyName("player")]
        public ApiFootballLineupPlayer? Player { get; set; }
    }

    internal sealed class ApiFootballLineupPlayer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public int? Number { get; set; }

        [JsonPropertyName("pos")]
        public string? Position { get; set; }

        [JsonPropertyName("grid")]
        public string? Grid { get; set; }

        [JsonPropertyName("captain")]
        public bool Captain { get; set; }
    }

    internal sealed class ApiFootballTeam
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    internal sealed class ApiFootballCoach
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
