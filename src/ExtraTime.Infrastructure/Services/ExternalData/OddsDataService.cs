using System.Globalization;
using System.Net;
using System.Text;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.ExternalData;

/// <summary>
/// Imports historical betting odds and match statistics from Football-Data.co.uk CSV files.
/// </summary>
public sealed class OddsDataService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    ILogger<OddsDataService> logger) : IOddsDataService
{
    private static readonly Dictionary<string, string> LeagueFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PL"] = "E0",
        ["ELC"] = "E1",
        ["PD"] = "SP1",
        ["BL1"] = "D1",
        ["SA"] = "I1",
        ["FL1"] = "F1",
        ["DED"] = "N1",
        ["PPL"] = "P1"
    };

    public async Task ImportSeasonOddsAsync(
        string leagueCode,
        string season,
        DateTime? importedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (!LeagueFiles.TryGetValue(leagueCode, out var leagueFile))
        {
            logger.LogWarning("League {LeagueCode} is not supported by Football-Data.co.uk", leagueCode);
            return;
        }

        var seasonCode = ToSeasonCode(season);
        var client = httpClientFactory.CreateClient("FootballDataUk");

        try
        {
            var csvContent = await client.GetStringAsync($"/mmz4281/{seasonCode}/{leagueFile}.csv", cancellationToken);
            var rows = ParseCsv(csvContent);

            await SaveOddsAsync(rows, importedAtUtc, cancellationToken);

            logger.LogInformation(
                "Imported {RowCount} odds rows for league {LeagueCode} season {Season}",
                rows.Count,
                leagueCode,
                season);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("Odds file was not found for league {LeagueCode} season {Season}", leagueCode, season);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed importing odds for league {LeagueCode} season {Season}", leagueCode, season);
        }
    }

    public async Task ImportAllLeaguesAsync(CancellationToken cancellationToken = default)
    {
        var season = GetCurrentSeasonYear().ToString(CultureInfo.InvariantCulture);
        var leagueCodes = LeagueFiles.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

        for (var i = 0; i < leagueCodes.Length; i++)
        {
            await ImportSeasonOddsAsync(leagueCodes[i], season, cancellationToken: cancellationToken);
            if (i < leagueCodes.Length - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }
    }

    public async Task ImportHistoricalSeasonsAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        DateTime? importedAtUtc = null,
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
            await ImportSeasonOddsAsync(
                leagueCode,
                season.ToString(CultureInfo.InvariantCulture),
                importedAtUtc,
                cancellationToken);
        }
    }

    public async Task<MatchOdds?> GetOddsForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        return await context.MatchOdds
            .FirstOrDefaultAsync(o => o.MatchId == matchId, cancellationToken);
    }

    public async Task<MatchOdds?> GetOddsForMatchAsOfAsync(
        Guid matchId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        return await context.MatchOdds
            .Where(o => o.MatchId == matchId && o.ImportedAt <= asOfUtc)
            .OrderByDescending(o => o.ImportedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private List<OddsCsvRow> ParseCsv(string csvContent)
    {
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return [];
        }

        var headers = ParseCsvLine(lines[0]);
        var map = headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .ToDictionary(x => x.Header, x => x.Index, StringComparer.OrdinalIgnoreCase);

        if (!map.ContainsKey("Date") || !map.ContainsKey("HomeTeam") || !map.ContainsKey("AwayTeam"))
        {
            logger.LogWarning("Football-Data.co.uk CSV is missing required columns");
            return [];
        }

        var rows = new List<OddsCsvRow>(lines.Length - 1);
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var values = ParseCsvLine(lines[i]);
            if (values.Length == 0)
            {
                continue;
            }

            try
            {
                var row = new OddsCsvRow
                {
                    Date = ParseDate(GetValue(values, map, "Date")),
                    HomeTeam = GetValue(values, map, "HomeTeam"),
                    AwayTeam = GetValue(values, map, "AwayTeam"),
                    HomeGoals = ParseIntOrNull(GetValue(values, map, "FTHG")),
                    AwayGoals = ParseIntOrNull(GetValue(values, map, "FTAG")),
                    HomeOdds = ParseDoubleOrNull(GetValue(values, map, "B365H"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "BWH"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "AvgH")),
                    DrawOdds = ParseDoubleOrNull(GetValue(values, map, "B365D"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "BWD"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "AvgD")),
                    AwayOdds = ParseDoubleOrNull(GetValue(values, map, "B365A"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "BWA"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "AvgA")),
                    Over25 = ParseDoubleOrNull(GetValue(values, map, "B365>2.5"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "Avg>2.5")),
                    Under25 = ParseDoubleOrNull(GetValue(values, map, "B365<2.5"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "Avg<2.5")),
                    BttsYes = ParseDoubleOrNull(GetValue(values, map, "B365BTSY"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "AvgBTSY")),
                    BttsNo = ParseDoubleOrNull(GetValue(values, map, "B365BTSN"))
                        ?? ParseDoubleOrNull(GetValue(values, map, "AvgBTSN")),
                    HomeHalfTimeGoals = ParseIntOrNull(GetValue(values, map, "HTHG")),
                    AwayHalfTimeGoals = ParseIntOrNull(GetValue(values, map, "HTAG")),
                    HomeShots = ParseIntOrNull(GetValue(values, map, "HS")),
                    HomeShotsOnTarget = ParseIntOrNull(GetValue(values, map, "HST")),
                    AwayShots = ParseIntOrNull(GetValue(values, map, "AS")),
                    AwayShotsOnTarget = ParseIntOrNull(GetValue(values, map, "AST")),
                    HomeCorners = ParseIntOrNull(GetValue(values, map, "HC")),
                    AwayCorners = ParseIntOrNull(GetValue(values, map, "AC")),
                    HomeFouls = ParseIntOrNull(GetValue(values, map, "HF")),
                    AwayFouls = ParseIntOrNull(GetValue(values, map, "AF")),
                    HomeYellowCards = ParseIntOrNull(GetValue(values, map, "HY")),
                    AwayYellowCards = ParseIntOrNull(GetValue(values, map, "AY")),
                    HomeRedCards = ParseIntOrNull(GetValue(values, map, "HR")),
                    AwayRedCards = ParseIntOrNull(GetValue(values, map, "AR")),
                    Referee = ParseStringOrNull(GetValue(values, map, "Referee"))
                };

                if (row.Date.HasValue && row.HomeOdds.HasValue && row.DrawOdds.HasValue && row.AwayOdds.HasValue)
                {
                    rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed parsing odds row at index {RowIndex}", i);
            }
        }

        return rows;
    }

    private async Task SaveOddsAsync(
        IReadOnlyList<OddsCsvRow> rows,
        DateTime? importedAtUtc,
        CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            if (!row.Date.HasValue)
            {
                continue;
            }

            var match = await FindMatchAsync(row.HomeTeam, row.AwayTeam, row.Date.Value, cancellationToken);
            if (match is null)
            {
                logger.LogDebug("No match found for imported row {HomeTeam} vs {AwayTeam} on {Date}", row.HomeTeam, row.AwayTeam, row.Date);
                continue;
            }

            var odds = await context.MatchOdds
                .FirstOrDefaultAsync(o => o.MatchId == match.Id, cancellationToken);

            if (odds is null)
            {
                odds = new MatchOdds
                {
                    MatchId = match.Id
                };
                context.MatchOdds.Add(odds);
            }

            odds.HomeWinOdds = row.HomeOdds!.Value;
            odds.DrawOdds = row.DrawOdds!.Value;
            odds.AwayWinOdds = row.AwayOdds!.Value;
            odds.Over25Odds = row.Over25;
            odds.Under25Odds = row.Under25;
            odds.BttsYesOdds = row.BttsYes;
            odds.BttsNoOdds = row.BttsNo;
            odds.DataSource = "football-data.co.uk";
            odds.ImportedAt = importedAtUtc ?? row.Date.Value.Date;
            odds.CalculateProbabilities();

            if (!HasAnyStats(row))
            {
                continue;
            }

            var stats = await context.MatchStats
                .FirstOrDefaultAsync(s => s.MatchId == match.Id, cancellationToken);

            if (stats is null)
            {
                stats = new MatchStats
                {
                    MatchId = match.Id
                };
                context.MatchStats.Add(stats);
            }

            stats.HomeShots = row.HomeShots;
            stats.HomeShotsOnTarget = row.HomeShotsOnTarget;
            stats.AwayShots = row.AwayShots;
            stats.AwayShotsOnTarget = row.AwayShotsOnTarget;
            stats.HomeHalfTimeGoals = row.HomeHalfTimeGoals;
            stats.AwayHalfTimeGoals = row.AwayHalfTimeGoals;
            stats.HomeCorners = row.HomeCorners;
            stats.AwayCorners = row.AwayCorners;
            stats.HomeFouls = row.HomeFouls;
            stats.AwayFouls = row.AwayFouls;
            stats.HomeYellowCards = row.HomeYellowCards;
            stats.AwayYellowCards = row.AwayYellowCards;
            stats.HomeRedCards = row.HomeRedCards;
            stats.AwayRedCards = row.AwayRedCards;
            stats.Referee = row.Referee;
            stats.DataSource = "football-data.co.uk";
            stats.ImportedAt = importedAtUtc ?? row.Date.Value.Date;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Match?> FindMatchAsync(
        string homeTeam,
        string awayTeam,
        DateTime date,
        CancellationToken cancellationToken)
    {
        var normalizedHome = NormalizeTeamName(homeTeam);
        var normalizedAway = NormalizeTeamName(awayTeam);
        var startDate = date.Date.AddDays(-1);
        var endDate = date.Date.AddDays(2);

        var matches = await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.MatchDateUtc >= startDate && m.MatchDateUtc < endDate)
            .ToListAsync(cancellationToken);

        return matches.FirstOrDefault(match =>
            IsTeamMatch(match.HomeTeam, normalizedHome) &&
            IsTeamMatch(match.AwayTeam, normalizedAway));
    }

    private static bool IsTeamMatch(Team team, string normalizedTarget)
    {
        var normalizedName = NormalizeTeamName(team.Name);
        var normalizedShortName = NormalizeTeamName(team.ShortName);

        return normalizedName == normalizedTarget ||
               normalizedShortName == normalizedTarget ||
               normalizedName.Contains(normalizedTarget, StringComparison.Ordinal) ||
               normalizedTarget.Contains(normalizedName, StringComparison.Ordinal) ||
               normalizedShortName.Contains(normalizedTarget, StringComparison.Ordinal) ||
               normalizedTarget.Contains(normalizedShortName, StringComparison.Ordinal);
    }

    private static string NormalizeTeamName(string name)
    {
        var normalized = new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return normalized
            .Replace("footballclub", string.Empty, StringComparison.Ordinal)
            .Replace("afc", string.Empty, StringComparison.Ordinal)
            .Replace("fc", string.Empty, StringComparison.Ordinal)
            .Replace("cf", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static bool HasAnyStats(OddsCsvRow row)
    {
        return row.HomeShots.HasValue ||
               row.AwayShots.HasValue ||
               row.HomeShotsOnTarget.HasValue ||
               row.AwayShotsOnTarget.HasValue ||
               row.HomeHalfTimeGoals.HasValue ||
               row.AwayHalfTimeGoals.HasValue ||
               row.HomeCorners.HasValue ||
               row.AwayCorners.HasValue ||
               row.HomeFouls.HasValue ||
               row.AwayFouls.HasValue ||
               row.HomeYellowCards.HasValue ||
               row.AwayYellowCards.HasValue ||
               row.HomeRedCards.HasValue ||
               row.AwayRedCards.HasValue ||
               row.Referee is not null;
    }

    private static string ToSeasonCode(string season)
    {
        if (season.Length == 4 && season.StartsWith("20", StringComparison.Ordinal) && int.TryParse(season, out var startYear))
        {
            return $"{startYear % 100:D2}{(startYear + 1) % 100:D2}";
        }

        return season;
    }

    private static int GetCurrentSeasonYear()
    {
        var utcNow = DateTime.UtcNow;
        return utcNow.Month >= 8 ? utcNow.Year : utcNow.Year - 1;
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var currentChar = line[i];
            if (currentChar == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (currentChar == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(currentChar);
        }

        values.Add(current.ToString().Trim());
        return [.. values];
    }

    private static string GetValue(string[] values, IReadOnlyDictionary<string, int> map, string column)
    {
        return map.TryGetValue(column, out var index) && index < values.Length
            ? values[index]
            : string.Empty;
    }

    private static DateTime? ParseDate(string value)
    {
        if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fullYearDate))
        {
            return DateTime.SpecifyKind(fullYearDate, DateTimeKind.Utc);
        }

        if (DateTime.TryParseExact(value, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var shortYearDate))
        {
            return DateTime.SpecifyKind(shortYearDate, DateTimeKind.Utc);
        }

        return null;
    }

    private static int? ParseIntOrNull(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static double? ParseDoubleOrNull(string value)
    {
        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string? ParseStringOrNull(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed class OddsCsvRow
    {
        public DateTime? Date { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public int? HomeGoals { get; set; }
        public int? AwayGoals { get; set; }
        public double? HomeOdds { get; set; }
        public double? DrawOdds { get; set; }
        public double? AwayOdds { get; set; }
        public double? Over25 { get; set; }
        public double? Under25 { get; set; }
        public double? BttsYes { get; set; }
        public double? BttsNo { get; set; }
        public int? HomeHalfTimeGoals { get; set; }
        public int? AwayHalfTimeGoals { get; set; }
        public int? HomeShots { get; set; }
        public int? HomeShotsOnTarget { get; set; }
        public int? AwayShots { get; set; }
        public int? AwayShotsOnTarget { get; set; }
        public int? HomeCorners { get; set; }
        public int? AwayCorners { get; set; }
        public int? HomeFouls { get; set; }
        public int? AwayFouls { get; set; }
        public int? HomeYellowCards { get; set; }
        public int? AwayYellowCards { get; set; }
        public int? HomeRedCards { get; set; }
        public int? AwayRedCards { get; set; }
        public string? Referee { get; set; }
    }
}
