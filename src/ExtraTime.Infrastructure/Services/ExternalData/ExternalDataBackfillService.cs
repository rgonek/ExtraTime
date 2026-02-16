using System.Globalization;
using System.Text.Json;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.ExternalData;

public sealed class ExternalDataBackfillService(
    IApplicationDbContext context,
    IUnderstatService understatService,
    IOddsDataService oddsDataService,
    IEloRatingService eloRatingService,
    IInjuryService injuryService,
    ILogger<ExternalDataBackfillService> logger) : IExternalDataBackfillService
{
    private static readonly JsonSerializerOptions JsonOptions = new();
    private const string UnderstatCheckpointPrefix = "Backfill:Understat:";
    private const string OddsCheckpointPrefix = "Backfill:Odds:";
    private const string EloCheckpointKey = "Backfill:Elo:Global";
    private const string InjuriesCheckpointKey = "Backfill:Injuries";

    public async Task BackfillForLeagueAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(leagueCode))
        {
            throw new ArgumentException("League code is required.", nameof(leagueCode));
        }

        if (fromSeason > toSeason)
        {
            throw new ArgumentException($"Invalid season range: {fromSeason} > {toSeason}", nameof(fromSeason));
        }

        var reports = new List<DataQualityReport>();
        var normalizedLeagueCode = leagueCode.Trim().ToUpperInvariant();
        var competitionId = await context.Competitions
            .Where(c => c.Code == normalizedLeagueCode)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!competitionId.HasValue)
        {
            logger.LogWarning("Backfill skipped because league {LeagueCode} does not exist", normalizedLeagueCode);
            return;
        }

        await BackfillUnderstatAsync(
            competitionId.Value,
            normalizedLeagueCode,
            fromSeason,
            toSeason,
            reports,
            cancellationToken);
        await BackfillOddsAsync(
            competitionId.Value,
            normalizedLeagueCode,
            fromSeason,
            toSeason,
            reports,
            cancellationToken);
        await CaptureInjurySnapshotsAsync(reports, cancellationToken);

        LogDataQualityReports("league", normalizedLeagueCode, reports);
    }

    public async Task BackfillGlobalEloAsync(
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default)
    {
        var fromDate = fromDateUtc.Date;
        var toDate = toDateUtc.Date;
        if (fromDate > toDate)
        {
            throw new ArgumentException($"Invalid date range: {fromDate:yyyy-MM-dd} > {toDate:yyyy-MM-dd}", nameof(fromDateUtc));
        }

        var lastCompletedDate = await GetLastCompletedDateAsync(EloCheckpointKey, cancellationToken);
        var startDate = lastCompletedDate.HasValue
            ? (lastCompletedDate.Value >= fromDate ? lastCompletedDate.Value.AddDays(1) : fromDate)
            : fromDate;

        if (startDate > toDate)
        {
            logger.LogInformation("Global Elo backfill already completed through {Date}", lastCompletedDate);
            return;
        }

        var daysProcessed = 0;
        var expectedRatings = 0;
        var availableRatings = 0;

        for (var currentDate = startDate; currentDate <= toDate; currentDate = currentDate.AddDays(1))
        {
            await eloRatingService.SyncEloRatingsForDateAsync(currentDate, cancellationToken);
            await SaveCheckpointDateAsync(EloCheckpointKey, currentDate, cancellationToken);

            var expectedForDate = await context.Teams.CountAsync(cancellationToken);
            var availableForDate = await context.TeamEloRatings
                .Where(x => x.RatingDate == currentDate)
                .CountAsync(cancellationToken);

            daysProcessed++;
            expectedRatings += expectedForDate;
            availableRatings += availableForDate;
        }

        var reports = new List<DataQualityReport>
        {
            BuildReport(
                source: "ClubElo",
                scope: $"{startDate:yyyy-MM-dd}..{toDate:yyyy-MM-dd}",
                expected: expectedRatings,
                available: availableRatings)
        };
        LogDataQualityReports("global-elo", daysProcessed.ToString(CultureInfo.InvariantCulture), reports);
    }

    private async Task BackfillUnderstatAsync(
        Guid competitionId,
        string leagueCode,
        int fromSeason,
        int toSeason,
        ICollection<DataQualityReport> reports,
        CancellationToken cancellationToken)
    {
        var checkpointKey = $"{UnderstatCheckpointPrefix}{leagueCode}";
        var lastCompletedSeason = await GetLastCompletedSeasonAsync(checkpointKey, cancellationToken);
        var startSeason = lastCompletedSeason.HasValue
            ? Math.Max(fromSeason, lastCompletedSeason.Value + 1)
            : fromSeason;

        for (var season = startSeason; season <= toSeason; season++)
        {
            var seasonText = season.ToString(CultureInfo.InvariantCulture);
            var seasonSnapshotDate = GetSeasonSnapshotDateUtc(season);

            await understatService.SyncLeagueXgStatsAsync(
                leagueCode,
                seasonText,
                seasonSnapshotDate,
                cancellationToken);

            await SaveCheckpointSeasonAsync(checkpointKey, season, cancellationToken);

            var expectedTeams = await context.CompetitionTeams
                .Where(x => x.CompetitionId == competitionId && x.Season == season)
                .Select(x => x.TeamId)
                .Distinct()
                .CountAsync(cancellationToken);

            var availableSnapshots = await context.TeamXgSnapshots
                .Where(x => x.CompetitionId == competitionId &&
                            x.Season == seasonText &&
                            x.SnapshotDateUtc == seasonSnapshotDate)
                .CountAsync(cancellationToken);

            reports.Add(BuildReport("Understat", $"{leagueCode}:{seasonText}", expectedTeams, availableSnapshots));
        }
    }

    private async Task BackfillOddsAsync(
        Guid competitionId,
        string leagueCode,
        int fromSeason,
        int toSeason,
        ICollection<DataQualityReport> reports,
        CancellationToken cancellationToken)
    {
        var checkpointKey = $"{OddsCheckpointPrefix}{leagueCode}";
        var lastCompletedSeason = await GetLastCompletedSeasonAsync(checkpointKey, cancellationToken);
        var startSeason = lastCompletedSeason.HasValue
            ? Math.Max(fromSeason, lastCompletedSeason.Value + 1)
            : fromSeason;

        for (var season = startSeason; season <= toSeason; season++)
        {
            var seasonText = season.ToString(CultureInfo.InvariantCulture);
            await oddsDataService.ImportSeasonOddsAsync(leagueCode, seasonText, cancellationToken: cancellationToken);
            await SaveCheckpointSeasonAsync(checkpointKey, season, cancellationToken);

            var seasonStart = new DateTime(season, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var seasonEnd = seasonStart.AddYears(1);

            var expectedMatches = await context.Matches
                .Where(m => m.CompetitionId == competitionId)
                .Where(m => m.MatchDateUtc >= seasonStart && m.MatchDateUtc < seasonEnd)
                .CountAsync(cancellationToken);

            var availableOdds = await context.MatchOdds
                .Join(
                    context.Matches,
                    odds => odds.MatchId,
                    match => match.Id,
                    (odds, match) => new { Odds = odds, Match = match })
                .Where(x => x.Match.CompetitionId == competitionId)
                .Where(x => x.Match.MatchDateUtc >= seasonStart && x.Match.MatchDateUtc < seasonEnd)
                .CountAsync(cancellationToken);

            reports.Add(BuildReport("Football-Data.co.uk", $"{leagueCode}:{seasonText}", expectedMatches, availableOdds));
        }
    }

    private async Task CaptureInjurySnapshotsAsync(
        ICollection<DataQualityReport> reports,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var lastCompletedDate = await GetLastCompletedDateAsync(InjuriesCheckpointKey, cancellationToken);
        if (lastCompletedDate.HasValue && lastCompletedDate.Value >= today)
        {
            logger.LogInformation("Injury snapshot backfill already completed for {Date}", today);
            return;
        }

        await injuryService.SyncInjuriesForUpcomingMatchesAsync(3, cancellationToken);
        await SaveCheckpointDateAsync(InjuriesCheckpointKey, today, cancellationToken);

        var availableSnapshots = await context.TeamInjurySnapshots
            .Where(s => s.SnapshotDateUtc == today)
            .CountAsync(cancellationToken);
        var expectedSnapshots = await context.TeamInjuries.CountAsync(cancellationToken);
        reports.Add(BuildReport("API-Football", $"{today:yyyy-MM-dd}", expectedSnapshots, availableSnapshots));
    }

    private void LogDataQualityReports(string runType, string scope, IReadOnlyCollection<DataQualityReport> reports)
    {
        foreach (var report in reports)
        {
            logger.LogInformation(
                "Backfill data quality [{RunType}:{Scope}] {Source} {Segment}: coverage {CoveragePercent:F1}% ({Available}/{Expected}), missing {MissingRatePercent:F1}%",
                runType,
                scope,
                report.Source,
                report.Scope,
                report.CoveragePercent,
                report.Available,
                report.Expected,
                report.MissingRatePercent);
        }
    }

    private static DateTime GetSeasonSnapshotDateUtc(int season)
    {
        return new DateTime(season + 1, 6, 30, 0, 0, 0, DateTimeKind.Utc);
    }

    private async Task<int?> GetLastCompletedSeasonAsync(string checkpointKey, CancellationToken cancellationToken)
    {
        var checkpoint = await context.IntegrationStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IntegrationName == checkpointKey, cancellationToken);
        if (checkpoint is null || string.IsNullOrWhiteSpace(checkpoint.LastErrorDetails))
        {
            return null;
        }

        return DeserializeCheckpoint(checkpoint.LastErrorDetails).LastCompletedSeason;
    }

    private async Task<DateTime?> GetLastCompletedDateAsync(string checkpointKey, CancellationToken cancellationToken)
    {
        var checkpoint = await context.IntegrationStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IntegrationName == checkpointKey, cancellationToken);
        if (checkpoint is null || string.IsNullOrWhiteSpace(checkpoint.LastErrorDetails))
        {
            return null;
        }

        return DeserializeCheckpoint(checkpoint.LastErrorDetails).LastCompletedDateUtc;
    }

    private async Task SaveCheckpointSeasonAsync(
        string checkpointKey,
        int season,
        CancellationToken cancellationToken)
    {
        var status = await GetOrCreateCheckpointStatusAsync(checkpointKey, cancellationToken);
        var payload = DeserializeCheckpoint(status.LastErrorDetails);
        payload.LastCompletedSeason = season;
        payload.LastCompletedDateUtc = null;
        ApplyCheckpointPayload(status, payload);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveCheckpointDateAsync(
        string checkpointKey,
        DateTime dateUtc,
        CancellationToken cancellationToken)
    {
        var status = await GetOrCreateCheckpointStatusAsync(checkpointKey, cancellationToken);
        var payload = DeserializeCheckpoint(status.LastErrorDetails);
        payload.LastCompletedDateUtc = dateUtc.Date;
        payload.LastCompletedSeason = null;
        ApplyCheckpointPayload(status, payload);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<IntegrationStatus> GetOrCreateCheckpointStatusAsync(
        string checkpointKey,
        CancellationToken cancellationToken)
    {
        var existing = await context.IntegrationStatuses
            .FirstOrDefaultAsync(x => x.IntegrationName == checkpointKey, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var now = DateTime.UtcNow;
        var created = new IntegrationStatus
        {
            IntegrationName = checkpointKey,
            Health = IntegrationHealth.Unknown,
            StaleThreshold = TimeSpan.FromDays(3650),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.IntegrationStatuses.Add(created);
        await context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static BackfillCheckpoint DeserializeCheckpoint(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new BackfillCheckpoint();
        }

        return JsonSerializer.Deserialize<BackfillCheckpoint>(payload, JsonOptions) ?? new BackfillCheckpoint();
    }

    private static void ApplyCheckpointPayload(IntegrationStatus status, BackfillCheckpoint checkpoint)
    {
        var now = DateTime.UtcNow;
        status.LastErrorDetails = JsonSerializer.Serialize(checkpoint, JsonOptions);
        status.LastAttemptedSync = now;
        status.LastSuccessfulSync = now;
        status.DataFreshAsOf = now;
        status.Health = IntegrationHealth.Healthy;
        status.UpdatedAt = now;
    }

    private static DataQualityReport BuildReport(string source, string scope, int expected, int available)
    {
        var safeExpected = Math.Max(0, expected);
        var safeAvailable = Math.Max(0, available);
        var coveragePercent = safeExpected == 0 ? 100 : Math.Min(100, (double)safeAvailable / safeExpected * 100);
        var missingRatePercent = safeExpected == 0 ? 0 : Math.Max(0, (double)(safeExpected - safeAvailable) / safeExpected * 100);

        return new DataQualityReport(
            source,
            scope,
            safeExpected,
            safeAvailable,
            coveragePercent,
            missingRatePercent);
    }

    private sealed record BackfillCheckpoint
    {
        public int? LastCompletedSeason { get; set; }
        public DateTime? LastCompletedDateUtc { get; set; }
    }

    private sealed record DataQualityReport(
        string Source,
        string Scope,
        int Expected,
        int Available,
        double CoveragePercent,
        double MissingRatePercent);
}
