using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.ExternalData;

/// <summary>
/// Scrapes expected goals data from Understat pages.
/// </summary>
public sealed partial class UnderstatService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    ILogger<UnderstatService> logger) : IUnderstatService
{
    private static readonly Dictionary<string, string> LeagueMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PL"] = "EPL",
        ["PD"] = "La_liga",
        ["BL1"] = "Bundesliga",
        ["SA"] = "Serie_A",
        ["FL1"] = "Ligue_1"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(2);

    public async Task<List<TeamXgStats>> SyncLeagueXgStatsAsync(
        string leagueCode,
        string season,
        CancellationToken cancellationToken = default)
    {
        if (!LeagueMapping.TryGetValue(leagueCode, out var understatLeague))
        {
            logger.LogWarning("League {LeagueCode} is not supported by Understat sync", leagueCode);
            return [];
        }

        var competition = await context.Competitions
            .FirstOrDefaultAsync(c => c.Code == leagueCode, cancellationToken);

        if (competition is null)
        {
            logger.LogWarning("Competition with code {LeagueCode} was not found", leagueCode);
            return [];
        }

        var client = httpClientFactory.CreateClient("Understat");
        var html = await client.GetStringAsync($"/league/{understatLeague}/{season}", cancellationToken);

        var teamsData = ParseTeamStats(html);
        if (teamsData.Count == 0)
        {
            return [];
        }

        var competitionTeams = await context.CompetitionTeams
            .Where(ct => ct.CompetitionId == competition.Id)
            .Select(ct => ct.Team)
            .ToListAsync(cancellationToken);

        var syncedAt = DateTime.UtcNow;
        var syncedStats = new List<TeamXgStats>();

        foreach (var understatTeam in teamsData)
        {
            var team = MatchTeam(understatTeam.TeamName, competitionTeams);
            if (team is null)
            {
                logger.LogDebug("Skipping unmatched Understat team {TeamName}", understatTeam.TeamName);
                continue;
            }

            var existing = await context.TeamXgStats
                .FirstOrDefaultAsync(
                    x => x.TeamId == team.Id &&
                         x.CompetitionId == competition.Id &&
                         x.Season == season,
                    cancellationToken);

            if (existing is null)
            {
                existing = new TeamXgStats
                {
                    TeamId = team.Id,
                    CompetitionId = competition.Id,
                    Season = season
                };
                context.TeamXgStats.Add(existing);
            }

            UpdateTeamStats(existing, understatTeam, syncedAt);
            syncedStats.Add(existing);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Synced Understat xG data for {LeagueCode} {Season}. Updated {TeamCount} teams.",
            leagueCode,
            season,
            syncedStats.Count);

        return syncedStats;
    }

    public async Task SyncLeagueSeasonRangeAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        CancellationToken cancellationToken = default)
    {
        if (fromSeason > toSeason)
        {
            throw new ArgumentException(
                $"Invalid season range: {fromSeason} > {toSeason}",
                nameof(fromSeason));
        }

        for (var season = fromSeason; season <= toSeason; season++)
        {
            await SyncLeagueXgStatsAsync(leagueCode, season.ToString(CultureInfo.InvariantCulture), cancellationToken);
            if (season < toSeason)
            {
                await Task.Delay(RequestDelay, cancellationToken);
            }
        }
    }

    public async Task<TeamXgStats?> GetTeamXgAsync(
        Guid teamId,
        Guid competitionId,
        string season,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamXgStats
            .FirstOrDefaultAsync(
                x => x.TeamId == teamId &&
                     x.CompetitionId == competitionId &&
                     x.Season == season,
                cancellationToken);
    }

    public async Task<TeamXgStats?> GetTeamXgAsOfAsync(
        Guid teamId,
        Guid competitionId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var maxSeason = GetSeasonYear(asOfUtc).ToString(CultureInfo.InvariantCulture);

        var candidates = await context.TeamXgStats
            .Where(x => x.TeamId == teamId &&
                        x.CompetitionId == competitionId &&
                        x.LastSyncedAt <= asOfUtc)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(x => string.CompareOrdinal(x.Season, maxSeason) <= 0)
            .OrderByDescending(x => x.Season)
            .ThenByDescending(x => x.LastSyncedAt)
            .FirstOrDefault();
    }

    public async Task<MatchXgStats?> GetMatchXgAsync(
        int understatMatchId,
        CancellationToken cancellationToken = default)
    {
        return await context.MatchXgStats
            .FirstOrDefaultAsync(x => x.UnderstatMatchId == understatMatchId, cancellationToken);
    }

    public async Task SyncAllLeaguesAsync(CancellationToken cancellationToken = default)
    {
        var season = GetSeasonYear(DateTime.UtcNow).ToString(CultureInfo.InvariantCulture);
        var leagueCodes = LeagueMapping.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

        for (var i = 0; i < leagueCodes.Length; i++)
        {
            await SyncLeagueXgStatsAsync(leagueCodes[i], season, cancellationToken);
            if (i < leagueCodes.Length - 1)
            {
                await Task.Delay(RequestDelay, cancellationToken);
            }
        }
    }

    private List<UnderstatTeamData> ParseTeamStats(string html)
    {
        var match = TeamsDataRegex().Match(html);
        if (!match.Success)
        {
            logger.LogWarning("Understat response did not contain teamsData payload");
            return [];
        }

        var escapedJson = match.Groups["json"].Value.Replace("\\x", "\\u00", StringComparison.Ordinal);
        var json = Regex.Unescape(escapedJson);

        var payload = JsonSerializer.Deserialize<Dictionary<string, UnderstatTeamPayload>>(json, JsonOptions);
        if (payload is null)
        {
            return [];
        }

        return payload
            .Where(kvp => int.TryParse(kvp.Key, out _))
            .Select(kvp => new UnderstatTeamData(
                int.Parse(kvp.Key, CultureInfo.InvariantCulture),
                kvp.Value.Title,
                kvp.Value.History.Sum(h => h.Xg),
                kvp.Value.History.Sum(h => h.XgAgainst),
                kvp.Value.History.Sum(h => h.Scored),
                kvp.Value.History.Sum(h => h.Missed),
                kvp.Value.History.Count,
                kvp.Value.History.TakeLast(5).ToList()))
            .ToList();
    }

    private static Team? MatchTeam(string understatTeamName, IReadOnlyList<Team> competitionTeams)
    {
        var exact = competitionTeams.FirstOrDefault(team =>
            string.Equals(team.Name, understatTeamName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(team.ShortName, understatTeamName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        var normalizedUnderstatName = NormalizeTeamName(understatTeamName);
        return competitionTeams.FirstOrDefault(team =>
        {
            var normalizedName = NormalizeTeamName(team.Name);
            var normalizedShortName = NormalizeTeamName(team.ShortName);

            return normalizedName == normalizedUnderstatName ||
                   normalizedShortName == normalizedUnderstatName ||
                   normalizedName.Contains(normalizedUnderstatName, StringComparison.Ordinal) ||
                   normalizedUnderstatName.Contains(normalizedName, StringComparison.Ordinal) ||
                   normalizedShortName.Contains(normalizedUnderstatName, StringComparison.Ordinal) ||
                   normalizedUnderstatName.Contains(normalizedShortName, StringComparison.Ordinal);
        });
    }

    private static string NormalizeTeamName(string name)
    {
        var normalized = Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]", string.Empty);
        return normalized
            .Replace("footballclub", string.Empty, StringComparison.Ordinal)
            .Replace("afc", string.Empty, StringComparison.Ordinal)
            .Replace("fc", string.Empty, StringComparison.Ordinal)
            .Replace("cf", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static void UpdateTeamStats(TeamXgStats entity, UnderstatTeamData data, DateTime syncedAt)
    {
        entity.UnderstatTeamId = data.UnderstatTeamId;
        entity.XgFor = data.XgFor;
        entity.XgAgainst = data.XgAgainst;
        entity.XgDiff = data.XgFor - data.XgAgainst;
        entity.XgPerMatch = data.MatchesPlayed > 0 ? data.XgFor / data.MatchesPlayed : 0;
        entity.XgAgainstPerMatch = data.MatchesPlayed > 0 ? data.XgAgainst / data.MatchesPlayed : 0;
        entity.GoalsScored = data.GoalsScored;
        entity.GoalsConceded = data.GoalsConceded;
        entity.XgOverperformance = data.GoalsScored - data.XgFor;
        entity.XgaOverperformance = data.XgAgainst - data.GoalsConceded;
        entity.RecentXgPerMatch = data.RecentMatches.Count > 0
            ? data.RecentMatches.Average(m => m.Xg)
            : 0;
        entity.RecentXgAgainstPerMatch = data.RecentMatches.Count > 0
            ? data.RecentMatches.Average(m => m.XgAgainst)
            : 0;
        entity.MatchesPlayed = data.MatchesPlayed;
        entity.LastSyncedAt = syncedAt;
    }

    private static int GetSeasonYear(DateTime utcDate)
    {
        return utcDate.Month < 8 ? utcDate.Year - 1 : utcDate.Year;
    }

    [GeneratedRegex(@"teamsData\s*=\s*JSON\.parse\('(?<json>.*?)'\);", RegexOptions.Singleline)]
    private static partial Regex TeamsDataRegex();

    private sealed record UnderstatTeamData(
        int UnderstatTeamId,
        string TeamName,
        double XgFor,
        double XgAgainst,
        int GoalsScored,
        int GoalsConceded,
        int MatchesPlayed,
        IReadOnlyList<UnderstatMatchHistoryPayload> RecentMatches);

    private sealed class UnderstatTeamPayload
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("history")]
        public List<UnderstatMatchHistoryPayload> History { get; set; } = [];
    }

    private sealed class UnderstatMatchHistoryPayload
    {
        [JsonPropertyName("xG")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double Xg { get; set; }

        [JsonPropertyName("xGA")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double XgAgainst { get; set; }

        [JsonPropertyName("scored")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Scored { get; set; }

        [JsonPropertyName("missed")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Missed { get; set; }
    }
}
