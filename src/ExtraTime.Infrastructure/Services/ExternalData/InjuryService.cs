using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExtraTime.Infrastructure.Services.ExternalData;

/// <summary>
/// Fetches injury data from API-Football while honoring free-tier quota limits.
/// </summary>
public sealed class InjuryService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    IOptions<ApiFootballSettings> settings,
    ILogger<InjuryService> logger) : IInjuryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly object DailyQuotaLock = new();
    private static DateTime _requestCounterDateUtc = DateTime.UtcNow.Date;
    private static int _dailyRequestCount;

    public async Task SyncInjuriesForUpcomingMatchesAsync(
        int daysAhead = 3,
        CancellationToken cancellationToken = default)
    {
        if (daysAhead < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(daysAhead), "Days ahead must be at least 1.");
        }

        var apiSettings = settings.Value;
        if (!apiSettings.Enabled)
        {
            logger.LogDebug("API-Football injury sync skipped because it is disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(apiSettings.ApiKey))
        {
            logger.LogWarning("API-Football injury sync skipped because API key is missing.");
            return;
        }

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(daysAhead);

        var matches = await context.Matches
            .Where(m => m.MatchDateUtc >= now && m.MatchDateUtc <= cutoff)
            .Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Timed)
            .Select(m => new { m.HomeTeamId, m.AwayTeamId })
            .ToListAsync(cancellationToken);

        var teamIds = matches
            .SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId })
            .Distinct()
            .ToList();

        if (teamIds.Count == 0)
        {
            logger.LogDebug("No upcoming teams found for injury sync.");
            return;
        }

        var staleCutoff = now.AddHours(-Math.Max(1, apiSettings.StaleAfterHours));
        var existingInjuries = await context.TeamInjuries
            .Where(t => teamIds.Contains(t.TeamId))
            .ToDictionaryAsync(t => t.TeamId, cancellationToken);

        var teamsToSync = teamIds
            .Where(teamId =>
                !existingInjuries.TryGetValue(teamId, out var existing) ||
                existing.LastSyncedAt < staleCutoff)
            .ToList();

        if (teamsToSync.Count == 0)
        {
            logger.LogDebug("No stale injury records require syncing.");
            return;
        }

        var remainingBudget = GetRemainingRequestBudget(apiSettings);
        logger.LogInformation(
            "Injury sync queued for {TeamCount} teams with {RemainingBudget} quota slots available.",
            teamsToSync.Count,
            remainingBudget);

        foreach (var teamId in teamsToSync)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryConsumeRequestSlot(apiSettings, out var remainingAfterConsume))
            {
                logger.LogInformation(
                    "Injury sync stopped early due to quota reservation for lineups. Remaining teams skipped: {RemainingTeams}",
                    teamsToSync.Count - teamsToSync.IndexOf(teamId));
                break;
            }

            try
            {
                var synced = await SyncTeamInjuriesAsync(teamId, apiSettings, cancellationToken);
                if (!synced)
                {
                    logger.LogDebug("No injury update persisted for team {TeamId}", teamId);
                }
                else
                {
                    logger.LogDebug(
                        "Injury sync completed for team {TeamId}; remaining quota slots: {Remaining}",
                        teamId,
                        remainingAfterConsume);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Failed API-Football injury request for team {TeamId}", teamId);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse API-Football injury response for team {TeamId}", teamId);
            }
        }
    }

    public async Task<TeamInjuries?> GetTeamInjuriesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamInjuries
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TeamId == teamId, cancellationToken);
    }

    public async Task<TeamInjuries?> GetTeamInjuriesAsOfAsync(
        Guid teamId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = asOfUtc.Date;
        return await context.TeamInjurySnapshots
            .AsNoTracking()
            .Where(t => t.TeamId == teamId && t.SnapshotDateUtc <= asOfDate)
            .OrderByDescending(t => t.SnapshotDateUtc)
            .Select(t => new TeamInjuries
            {
                TeamId = t.TeamId,
                TotalInjured = t.TotalInjured,
                KeyPlayersInjured = t.KeyPlayersInjured,
                InjuryImpactScore = t.InjuryImpactScore,
                InjuredPlayerNames = t.InjuredPlayerNames,
                LastSyncedAt = t.SnapshotDateUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public double CalculateInjuryImpact(TeamInjuries injuries)
    {
        var impact = 0d;
        impact += injuries.TotalInjured * 5;
        impact += injuries.KeyPlayersInjured * 15;
        impact += injuries.LongTermInjuries * 8;
        impact += injuries.Doubtful * 2;
        impact += injuries.TopScorerInjured ? 20 : 0;
        impact += injuries.CaptainInjured ? 10 : 0;
        impact += injuries.FirstChoiceGkInjured ? 15 : 0;

        return Math.Min(100, impact);
    }

    private async Task<bool> SyncTeamInjuriesAsync(
        Guid teamId,
        ApiFootballSettings apiSettings,
        CancellationToken cancellationToken)
    {
        var team = await context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            logger.LogWarning("Team {TeamId} not found for injury sync.", teamId);
            return false;
        }

        var seasonYear = GetCurrentSeasonYear(DateTime.UtcNow);
        var client = httpClientFactory.CreateClient("ApiFootball");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/v3/injuries?team={team.ExternalId}&season={seasonYear}");
        request.Headers.TryAddWithoutValidation("X-RapidAPI-Key", apiSettings.ApiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "API-Football injury request failed for team {TeamId} with status {StatusCode}.",
                teamId,
                response.StatusCode);
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiFootballInjuryResponse>(JsonOptions, cancellationToken);
        if (payload?.Response is null)
        {
            logger.LogWarning("API-Football returned an empty injuries payload for team {TeamId}.", teamId);
            return false;
        }

        await ProcessInjuryResponseAsync(teamId, payload.Response, apiSettings.StaleAfterHours, cancellationToken);
        return true;
    }

    private async Task ProcessInjuryResponseAsync(
        Guid teamId,
        IReadOnlyList<ApiFootballInjury> sourceInjuries,
        int staleAfterHours,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var existingPlayerInjuries = await context.PlayerInjuries
            .Where(i => i.TeamId == teamId)
            .ToListAsync(cancellationToken);
        context.PlayerInjuries.RemoveRange(existingPlayerInjuries);

        var activeInjuries = sourceInjuries
            .Where(i => i.Player is not null)
            .Where(i => !IsSuspensionRelated(i.Player!.Reason))
            .Select(i => CreatePlayerInjury(teamId, i, now))
            .ToList();

        context.PlayerInjuries.AddRange(activeInjuries);

        var teamInjuries = await context.TeamInjuries
            .FirstOrDefaultAsync(t => t.TeamId == teamId, cancellationToken);

        if (teamInjuries is null)
        {
            teamInjuries = new TeamInjuries
            {
                TeamId = teamId
            };
            context.TeamInjuries.Add(teamInjuries);
        }

        var injuredNames = activeInjuries
            .Select(i => i.PlayerName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        teamInjuries.TotalInjured = activeInjuries.Count;
        teamInjuries.KeyPlayersInjured = activeInjuries.Count(i => i.IsKeyPlayer);
        teamInjuries.LongTermInjuries = activeInjuries.Count(i => i.InjurySeverity == "Severe");
        teamInjuries.ShortTermInjuries = activeInjuries.Count(i => i.InjurySeverity == "Minor");
        teamInjuries.Doubtful = activeInjuries.Count(i => i.IsDoubtful);
        teamInjuries.InjuredPlayerNames = JsonSerializer.Serialize(injuredNames);
        teamInjuries.TopScorerInjured = activeInjuries.Any(i => i.IsKeyPlayer && i.Position == "FWD");
        teamInjuries.CaptainInjured = activeInjuries.Any(i => i.InjuryType.Contains("captain", StringComparison.OrdinalIgnoreCase));
        teamInjuries.FirstChoiceGkInjured = activeInjuries.Any(i => i.Position == "GK" && i.IsActive);
        teamInjuries.LastSyncedAt = now;
        teamInjuries.NextSyncDue = now.AddHours(Math.Max(1, staleAfterHours));
        teamInjuries.InjuryImpactScore = CalculateInjuryImpact(teamInjuries);

        var snapshotDate = now.Date;
        var injurySnapshot = await context.TeamInjurySnapshots
            .FirstOrDefaultAsync(
                s => s.TeamId == teamId && s.SnapshotDateUtc == snapshotDate,
                cancellationToken);

        if (injurySnapshot is null)
        {
            injurySnapshot = new TeamInjurySnapshot
            {
                TeamId = teamId,
                SnapshotDateUtc = snapshotDate
            };
            context.TeamInjurySnapshots.Add(injurySnapshot);
        }

        injurySnapshot.TotalInjured = teamInjuries.TotalInjured;
        injurySnapshot.KeyPlayersInjured = teamInjuries.KeyPlayersInjured;
        injurySnapshot.InjuryImpactScore = teamInjuries.InjuryImpactScore;
        injurySnapshot.InjuredPlayerNames = teamInjuries.InjuredPlayerNames;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static PlayerInjury CreatePlayerInjury(Guid teamId, ApiFootballInjury source, DateTime now)
    {
        var position = MapPosition(source.Player!.Type);
        var injuryType = string.IsNullOrWhiteSpace(source.Player.Reason) ? "Unknown" : source.Player.Reason;
        var severity = MapSeverity(injuryType);
        var injuryDate = FromUnixTimestampOrNull(source.Fixture?.Timestamp);
        var isDoubtful = injuryType.Contains("doubt", StringComparison.OrdinalIgnoreCase) ||
                         injuryType.Contains("question", StringComparison.OrdinalIgnoreCase);

        return new PlayerInjury
        {
            TeamId = teamId,
            ExternalPlayerId = source.Player.Id,
            PlayerName = source.Player.Name ?? string.Empty,
            Position = position,
            IsKeyPlayer = position is "GK" or "FWD",
            InjuryType = injuryType,
            InjurySeverity = severity,
            InjuryDate = injuryDate,
            ExpectedReturn = EstimateExpectedReturn(injuryDate, severity, isDoubtful),
            IsDoubtful = isDoubtful,
            IsActive = true,
            LastUpdatedAt = now
        };
    }

    private static DateTime? EstimateExpectedReturn(DateTime? injuryDate, string severity, bool isDoubtful)
    {
        if (isDoubtful)
        {
            return injuryDate?.AddDays(3);
        }

        return severity switch
        {
            "Severe" => injuryDate?.AddDays(45),
            "Moderate" => injuryDate?.AddDays(14),
            "Minor" => injuryDate?.AddDays(5),
            _ => injuryDate?.AddDays(10)
        };
    }

    private static bool TryConsumeRequestSlot(ApiFootballSettings settings, out int remainingAfterConsume)
    {
        lock (DailyQuotaLock)
        {
            ResetDailyCounterIfNeeded();
            var maxRequests = GetInjuryQuotaLimit(settings);
            if (_dailyRequestCount >= maxRequests)
            {
                remainingAfterConsume = 0;
                return false;
            }

            _dailyRequestCount++;
            remainingAfterConsume = Math.Max(0, maxRequests - _dailyRequestCount);
            return true;
        }
    }

    private static int GetRemainingRequestBudget(ApiFootballSettings settings)
    {
        lock (DailyQuotaLock)
        {
            ResetDailyCounterIfNeeded();
            var maxRequests = GetInjuryQuotaLimit(settings);
            return Math.Max(0, maxRequests - _dailyRequestCount);
        }
    }

    private static void ResetDailyCounterIfNeeded()
    {
        var todayUtc = DateTime.UtcNow.Date;
        if (_requestCounterDateUtc == todayUtc)
        {
            return;
        }

        _requestCounterDateUtc = todayUtc;
        _dailyRequestCount = 0;
    }

    private static int GetInjuryQuotaLimit(ApiFootballSettings settings)
    {
        var maxDailyRequests = Math.Max(0, settings.MaxDailyRequests);
        if (!settings.SharedQuotaWithLineups)
        {
            return maxDailyRequests;
        }

        var reservedForLineups = Math.Max(0, settings.ReservedForLineupRequests);
        return Math.Max(0, maxDailyRequests - reservedForLineups);
    }

    private static int GetCurrentSeasonYear(DateTime utcNow)
    {
        return utcNow.Month >= 7 ? utcNow.Year : utcNow.Year - 1;
    }

    private static DateTime? FromUnixTimestampOrNull(long? unixTimestamp)
    {
        if (unixTimestamp is null or <= 0)
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value).UtcDateTime;
    }

    private static string MapPosition(string? value)
    {
        return value?.ToUpperInvariant() switch
        {
            "GOALKEEPER" => "GK",
            "DEFENDER" => "DEF",
            "MIDFIELDER" => "MID",
            "ATTACKER" => "FWD",
            _ => "UNK"
        };
    }

    private static string MapSeverity(string value)
    {
        if (value.Contains("acl", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("fracture", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("rupture", StringComparison.OrdinalIgnoreCase))
        {
            return "Severe";
        }

        if (value.Contains("strain", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("sprain", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("hamstring", StringComparison.OrdinalIgnoreCase))
        {
            return "Moderate";
        }

        return "Minor";
    }

    private static bool IsSuspensionRelated(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        return reason.Contains("suspension", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("suspended", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("missing", StringComparison.OrdinalIgnoreCase);
    }

    internal sealed class ApiFootballInjuryResponse
    {
        [JsonPropertyName("response")]
        public List<ApiFootballInjury>? Response { get; set; }
    }

    internal sealed class ApiFootballInjury
    {
        [JsonPropertyName("player")]
        public ApiFootballPlayer? Player { get; set; }

        [JsonPropertyName("fixture")]
        public ApiFootballFixture? Fixture { get; set; }
    }

    internal sealed class ApiFootballPlayer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }

    internal sealed class ApiFootballFixture
    {
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }
    }
}
