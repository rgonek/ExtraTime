using System.Diagnostics;
using System.Globalization;
using System.Text;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.ExternalData;

public sealed class EloRatingService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    IIntegrationHealthService healthService,
    ILogger<EloRatingService> logger) : IEloRatingService
{
    private static readonly Dictionary<string, string> TeamNameMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Man City"] = "Manchester City",
        ["Man United"] = "Manchester United",
        ["Spurs"] = "Tottenham Hotspur",
        ["Wolves"] = "Wolverhampton Wanderers",
        ["West Ham"] = "West Ham United",
        ["Sheffield Utd"] = "Sheffield United",
        ["Nott'm Forest"] = "Nottingham Forest",
        ["Newcastle"] = "Newcastle United",
        ["Leicester"] = "Leicester City",
        ["Leeds"] = "Leeds United",
        ["Brighton"] = "Brighton and Hove Albion",
        ["Athletic Bilbao"] = "Athletic Club",
        ["Inter"] = "Inter Milan",
        ["AC Milan"] = "Milan",
        ["PSG"] = "Paris Saint-Germain"
    };

    public Task SyncEloRatingsAsync(CancellationToken cancellationToken = default)
    {
        return SyncEloRatingsForDateAsync(DateTime.UtcNow.Date, cancellationToken);
    }

    public async Task SyncEloRatingsForDateAsync(
        DateTime dateUtc,
        CancellationToken cancellationToken = default)
    {
        var ratingDate = dateUtc.Date;
        var startedAt = Stopwatch.GetTimestamp();
        var dateSegment = ratingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            var client = httpClientFactory.CreateClient("ClubElo");
            var csvContent = await client.GetStringAsync($"/{dateSegment}", cancellationToken);
            var rows = ParseEloCsv(csvContent);
            var syncedCount = await SaveEloRatingsAsync(rows, ratingDate, cancellationToken);

            await healthService.RecordSuccessAsync(
                IntegrationType.ClubElo,
                Stopwatch.GetElapsedTime(startedAt),
                cancellationToken);

            logger.LogInformation("Synced {Count} ClubElo ratings for {RatingDate}", syncedCount, dateSegment);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await healthService.RecordFailureAsync(
                IntegrationType.ClubElo,
                ex.Message,
                ex.ToString(),
                cancellationToken);

            logger.LogError(ex, "Failed syncing ClubElo ratings for {RatingDate}", dateSegment);
            throw;
        }
    }

    public async Task BackfillEloRatingsAsync(
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default)
    {
        var fromDate = fromDateUtc.Date;
        var toDate = toDateUtc.Date;

        if (fromDate > toDate)
        {
            throw new ArgumentException(
                $"Invalid date range: {fromDate:yyyy-MM-dd} > {toDate:yyyy-MM-dd}",
                nameof(fromDateUtc));
        }

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            await SyncEloRatingsForDateAsync(date, cancellationToken);
        }
    }

    public async Task<TeamEloRating?> GetTeamEloAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamEloRatings
            .Where(r => r.TeamId == teamId)
            .OrderByDescending(r => r.RatingDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TeamEloRating?> GetTeamEloAtDateAsync(
        Guid teamId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = dateUtc.Date;
        return await context.TeamEloRatings
            .Where(r => r.TeamId == teamId && r.RatingDate <= asOfDate)
            .OrderByDescending(r => r.RatingDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static List<EloRatingRow> ParseEloCsv(string csvContent)
    {
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return [];
        }

        var rows = new List<EloRatingRow>(lines.Length - 1);
        for (var i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length < 5)
            {
                continue;
            }

            if (!int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rank))
            {
                continue;
            }

            if (!double.TryParse(values[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var elo))
            {
                continue;
            }

            var level = int.TryParse(values[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLevel)
                ? parsedLevel
                : 1;

            rows.Add(new EloRatingRow(rank, values[1].Trim(), level, elo));
        }

        return rows;
    }

    private async Task<int> SaveEloRatingsAsync(
        IReadOnlyList<EloRatingRow> rows,
        DateTime ratingDate,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        var teams = await context.Teams
            .ToListAsync(cancellationToken);

        var existingRatings = await context.TeamEloRatings
            .Where(r => r.RatingDate == ratingDate)
            .ToDictionaryAsync(r => r.TeamId, cancellationToken);

        var syncedAt = DateTime.UtcNow;
        var matchedCount = 0;

        foreach (var row in rows)
        {
            if (row.Level > 1)
            {
                continue;
            }

            var team = MatchTeam(row.ClubName, teams);
            if (team is null)
            {
                continue;
            }

            if (existingRatings.TryGetValue(team.Id, out var existing))
            {
                existing.EloRating = row.EloRating;
                existing.EloRank = row.Rank;
                existing.ClubEloName = row.ClubName;
                existing.SyncedAt = syncedAt;
                matchedCount++;
                continue;
            }

            var created = new TeamEloRating
            {
                TeamId = team.Id,
                EloRating = row.EloRating,
                EloRank = row.Rank,
                ClubEloName = row.ClubName,
                RatingDate = ratingDate,
                SyncedAt = syncedAt
            };

            context.TeamEloRatings.Add(created);
            existingRatings[team.Id] = created;
            matchedCount++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return matchedCount;
    }

    private static Team? MatchTeam(string clubEloName, IReadOnlyList<Team> teams)
    {
        var mappedName = TeamNameMapping.GetValueOrDefault(clubEloName, clubEloName);

        var exact = teams.FirstOrDefault(team =>
            string.Equals(team.Name, mappedName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(team.ShortName, mappedName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(team.Name, clubEloName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(team.ShortName, clubEloName, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            return exact;
        }

        var normalizedMapped = NormalizeTeamName(mappedName);
        var normalizedClubElo = NormalizeTeamName(clubEloName);

        return teams.FirstOrDefault(team =>
        {
            var normalizedName = NormalizeTeamName(team.Name);
            var normalizedShortName = NormalizeTeamName(team.ShortName);

            return normalizedName == normalizedMapped ||
                   normalizedShortName == normalizedMapped ||
                   normalizedName == normalizedClubElo ||
                   normalizedShortName == normalizedClubElo ||
                   normalizedName.Contains(normalizedMapped, StringComparison.Ordinal) ||
                   normalizedMapped.Contains(normalizedName, StringComparison.Ordinal) ||
                   normalizedShortName.Contains(normalizedMapped, StringComparison.Ordinal) ||
                   normalizedMapped.Contains(normalizedShortName, StringComparison.Ordinal);
        });
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

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var currentChar = line[i];
            if (currentChar == '\r')
            {
                continue;
            }

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

    private sealed record EloRatingRow(int Rank, string ClubName, int Level, double EloRating);
}
