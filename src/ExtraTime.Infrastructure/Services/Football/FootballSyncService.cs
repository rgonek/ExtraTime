using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class FootballSyncService(
    IApplicationDbContext context,
    IFootballDataService footballDataService,
    IJobDispatcher jobDispatcher,
    IOptions<FootballDataSettings> settings,
    ILogger<FootballSyncService> logger) : IFootballSyncService
{
    private readonly FootballDataSettings _settings = settings.Value;

    public async Task SyncCompetitionsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting competition sync for {Count} competitions", _settings.SupportedCompetitionIds.Length);

        foreach (var externalId in _settings.SupportedCompetitionIds)
        {
            var apiCompetition = await footballDataService.GetCompetitionAsync(externalId, ct);
            if (apiCompetition is null)
            {
                logger.LogWarning("Failed to fetch competition {ExternalId}", externalId);
                continue;
            }

            var competition = await context.Competitions
                .FirstOrDefaultAsync(c => c.ExternalId == externalId, ct);

            if (competition is null)
            {
                competition = new Competition
                {
                    ExternalId = apiCompetition.Id,
                    Name = apiCompetition.Name,
                    Code = apiCompetition.Code,
                    Country = apiCompetition.Area.Name,
                    LogoUrl = apiCompetition.Emblem,
                    CurrentMatchday = apiCompetition.CurrentSeason?.CurrentMatchday,
                    CurrentSeasonStart = apiCompetition.CurrentSeason?.StartDate,
                    CurrentSeasonEnd = apiCompetition.CurrentSeason?.EndDate,
                    LastSyncedAt = Clock.UtcNow
                };
                context.Competitions.Add(competition);
                logger.LogInformation("Added new competition: {Name}", competition.Name);
            }
            else
            {
                competition.Name = apiCompetition.Name;
                competition.Code = apiCompetition.Code;
                competition.Country = apiCompetition.Area.Name;
                competition.LogoUrl = apiCompetition.Emblem;
                competition.CurrentMatchday = apiCompetition.CurrentSeason?.CurrentMatchday;
                competition.CurrentSeasonStart = apiCompetition.CurrentSeason?.StartDate;
                competition.CurrentSeasonEnd = apiCompetition.CurrentSeason?.EndDate;
                competition.LastSyncedAt = Clock.UtcNow;
                logger.LogInformation("Updated competition: {Name}", competition.Name);
            }
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Competition sync completed");
    }

    public async Task SyncTeamsForCompetitionAsync(Guid competitionId, CancellationToken ct = default)
    {
        var competition = await context.Competitions
            .FirstOrDefaultAsync(c => c.Id == competitionId, ct);

        if (competition is null)
        {
            logger.LogWarning("Competition {CompetitionId} not found", competitionId);
            return;
        }

        logger.LogInformation("Starting team sync for competition: {Name}", competition.Name);

        var apiTeams = await footballDataService.GetTeamsForCompetitionAsync(competition.ExternalId, ct);

        var currentYear = Clock.UtcNow.Year;
        var season = competition.CurrentSeasonStart?.Year ?? currentYear;

        foreach (var apiTeam in apiTeams)
        {
            var team = await context.Teams
                .FirstOrDefaultAsync(t => t.ExternalId == apiTeam.Id, ct);

            if (team is null)
            {
                team = new Team
                {
                    ExternalId = apiTeam.Id,
                    Name = apiTeam.Name,
                    ShortName = apiTeam.ShortName,
                    Tla = apiTeam.Tla,
                    LogoUrl = apiTeam.Crest,
                    ClubColors = apiTeam.ClubColors,
                    Venue = apiTeam.Venue,
                    LastSyncedAt = Clock.UtcNow
                };
                context.Teams.Add(team);
                logger.LogInformation("Added new team: {Name}", team.Name);
            }
            else
            {
                team.Name = apiTeam.Name;
                team.ShortName = apiTeam.ShortName;
                team.Tla = apiTeam.Tla;
                team.LogoUrl = apiTeam.Crest;
                team.ClubColors = apiTeam.ClubColors;
                team.Venue = apiTeam.Venue;
                team.LastSyncedAt = Clock.UtcNow;
            }

            await context.SaveChangesAsync(ct);

            var existingLink = await context.CompetitionTeams
                .FirstOrDefaultAsync(ct2 =>
                    ct2.CompetitionId == competition.Id &&
                    ct2.TeamId == team.Id &&
                    ct2.Season == season, ct);

            if (existingLink is null)
            {
                var competitionTeam = new CompetitionTeam
                {
                    CompetitionId = competition.Id,
                    TeamId = team.Id,
                    Season = season
                };
                context.CompetitionTeams.Add(competitionTeam);
            }
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Team sync completed for competition: {Name}", competition.Name);
    }

    public async Task SyncMatchesAsync(DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default)
    {
        dateFrom ??= Clock.UtcNow.Date;
        dateTo ??= Clock.UtcNow.Date.AddDays(14);

        logger.LogInformation("Starting match sync from {DateFrom} to {DateTo}", dateFrom, dateTo);

        foreach (var externalCompetitionId in _settings.SupportedCompetitionIds)
        {
            var competition = await context.Competitions
                .FirstOrDefaultAsync(c => c.ExternalId == externalCompetitionId, ct);

            if (competition is null)
            {
                logger.LogWarning("Competition with external ID {ExternalId} not found in database", externalCompetitionId);
                continue;
            }

            var apiMatches = await footballDataService.GetMatchesForCompetitionAsync(
                externalCompetitionId, dateFrom, dateTo, ct);

            await ProcessMatchesAsync(apiMatches, competition, ct);
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Match sync completed");
    }

    public async Task SyncLiveMatchResultsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting live match sync");

        var liveMatches = await footballDataService.GetLiveMatchesAsync(ct);

        if (liveMatches.Count == 0)
        {
            logger.LogInformation("No live matches found");
            return;
        }

        foreach (var apiMatch in liveMatches)
        {
            var match = await context.Matches
                .FirstOrDefaultAsync(m => m.ExternalId == apiMatch.Id, ct);

            if (match is not null)
            {
                var previousStatus = match.Status;
                var newStatus = ParseMatchStatus(apiMatch.Status);

                match.UpdateStatus(newStatus);
                match.UpdateScore(
                    apiMatch.Score.FullTime.Home,
                    apiMatch.Score.FullTime.Away,
                    apiMatch.Score.HalfTime.Home,
                    apiMatch.Score.HalfTime.Away);

                logger.LogInformation("Updated live match: {MatchId} - Score: {Home}-{Away}",
                    match.Id, match.HomeScore, match.AwayScore);

                // If match just finished, trigger bet calculation
                if (previousStatus != MatchStatus.Finished && newStatus == MatchStatus.Finished)
                {
                    await jobDispatcher.EnqueueAsync(
                        "CalculateBetResults",
                        new { matchId = match.Id, competitionId = match.CompetitionId },
                        ct);
                    logger.LogInformation("Enqueued bet calculation job for match {MatchId}", match.Id);
                }
            }
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Live match sync completed");
    }

    private async Task ProcessMatchesAsync(
        IReadOnlyList<MatchApiDto> apiMatches,
        Competition competition,
        CancellationToken ct)
    {
        foreach (var apiMatch in apiMatches)
        {
            var homeTeam = await context.Teams
                .FirstOrDefaultAsync(t => t.ExternalId == apiMatch.HomeTeam.Id, ct);
            var awayTeam = await context.Teams
                .FirstOrDefaultAsync(t => t.ExternalId == apiMatch.AwayTeam.Id, ct);

            if (homeTeam is null || awayTeam is null)
            {
                logger.LogWarning("Teams not found for match {MatchId}. Home: {HomeId}, Away: {AwayId}",
                    apiMatch.Id, apiMatch.HomeTeam.Id, apiMatch.AwayTeam.Id);
                continue;
            }

            var match = await context.Matches
                .FirstOrDefaultAsync(m => m.ExternalId == apiMatch.Id, ct);

            if (match is null)
            {
                match = Match.Create(
                    apiMatch.Id,
                    competition.Id,
                    homeTeam.Id,
                    awayTeam.Id,
                    apiMatch.UtcDate,
                    ParseMatchStatus(apiMatch.Status),
                    apiMatch.Matchday,
                    apiMatch.Stage,
                    apiMatch.Group,
                    apiMatch.Venue);
                
                match.UpdateScore(
                    apiMatch.Score.FullTime.Home,
                    apiMatch.Score.FullTime.Away,
                    apiMatch.Score.HalfTime.Home,
                    apiMatch.Score.HalfTime.Away);

                context.Matches.Add(match);
                logger.LogInformation("Added new match: {Home} vs {Away} on {Date}",
                    homeTeam.Name, awayTeam.Name, match.MatchDateUtc);
            }
            else
            {
                var previousStatus = match.Status;
                var newStatus = ParseMatchStatus(apiMatch.Status);

                match.SyncDetails(
                    apiMatch.UtcDate,
                    newStatus,
                    apiMatch.Score.FullTime.Home,
                    apiMatch.Score.FullTime.Away,
                    apiMatch.Score.HalfTime.Home,
                    apiMatch.Score.HalfTime.Away);
                
                match.UpdateMetadata(
                    apiMatch.Matchday,
                    apiMatch.Stage,
                    apiMatch.Group,
                    apiMatch.Venue);

                // If match just finished, trigger bet calculation
                if (previousStatus != MatchStatus.Finished && newStatus == MatchStatus.Finished)
                {
                    await jobDispatcher.EnqueueAsync(
                        "CalculateBetResults",
                        new { matchId = match.Id, competitionId = match.CompetitionId },
                        ct);
                    logger.LogInformation("Enqueued bet calculation job for match {MatchId}", match.Id);
                }
            }
        }
    }

    private static MatchStatus ParseMatchStatus(string status) => status.ToUpperInvariant() switch
    {
        "SCHEDULED" => MatchStatus.Scheduled,
        "TIMED" => MatchStatus.Timed,
        "IN_PLAY" => MatchStatus.InPlay,
        "PAUSED" => MatchStatus.Paused,
        "FINISHED" => MatchStatus.Finished,
        "POSTPONED" => MatchStatus.Postponed,
        "SUSPENDED" => MatchStatus.Suspended,
        "CANCELLED" => MatchStatus.Cancelled,
        _ => MatchStatus.Scheduled
    };
}
