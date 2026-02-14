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
                competition = Competition.Create(
                    apiCompetition.Id,
                    apiCompetition.Name,
                    apiCompetition.Code,
                    apiCompetition.Area.Name,
                    apiCompetition.Emblem);
                competition.UpdateCurrentSeason(
                    apiCompetition.CurrentSeason?.CurrentMatchday,
                    apiCompetition.CurrentSeason?.StartDate,
                    apiCompetition.CurrentSeason?.EndDate);
                context.Competitions.Add(competition);
                logger.LogInformation("Added new competition: {Name}", competition.Name);
            }
            else
            {
                competition.UpdateDetails(
                    apiCompetition.Name,
                    apiCompetition.Code,
                    apiCompetition.Area.Name,
                    apiCompetition.Emblem);
                competition.UpdateCurrentSeason(
                    apiCompetition.CurrentSeason?.CurrentMatchday,
                    apiCompetition.CurrentSeason?.StartDate,
                    apiCompetition.CurrentSeason?.EndDate);
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
                team = Team.Create(
                    apiTeam.Id,
                    apiTeam.Name,
                    apiTeam.ShortName,
                    apiTeam.Tla,
                    apiTeam.Crest,
                    apiTeam.ClubColors,
                    apiTeam.Venue);
                context.Teams.Add(team);
                logger.LogInformation("Added new team: {Name}", team.Name);
            }
            else
            {
                team.UpdateDetails(
                    apiTeam.Name,
                    apiTeam.ShortName,
                    apiTeam.Tla,
                    apiTeam.Crest,
                    apiTeam.ClubColors,
                    apiTeam.Venue);
                team.RecordSync();
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

    public async Task SyncTeamsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default)
    {
        var competition = await context.Competitions
            .FirstOrDefaultAsync(c => c.ExternalId == competitionExternalId, ct);

        if (competition is null)
        {
            logger.LogWarning("Competition with external ID {ExternalId} not found", competitionExternalId);
            return;
        }

        logger.LogInformation("Starting team sync for competition: {Name}", competition.Name);

        var apiTeams = await footballDataService.GetTeamsForCompetitionAsync(competition.ExternalId, ct);
        var season = await GetCurrentSeasonAsync(competition.Id, ct);

        foreach (var apiTeam in apiTeams)
        {
            var team = await context.Teams
                .FirstOrDefaultAsync(t => t.ExternalId == apiTeam.Id, ct);

            if (team is null)
            {
                team = Team.Create(
                    apiTeam.Id,
                    apiTeam.Name,
                    apiTeam.ShortName,
                    apiTeam.Tla,
                    apiTeam.Crest,
                    apiTeam.ClubColors,
                    apiTeam.Venue);
                context.Teams.Add(team);
                logger.LogInformation("Added new team: {Name}", team.Name);
            }
            else
            {
                team.UpdateDetails(
                    apiTeam.Name,
                    apiTeam.ShortName,
                    apiTeam.Tla,
                    apiTeam.Crest,
                    apiTeam.ClubColors,
                    apiTeam.Venue);
                team.RecordSync();
            }

            await context.SaveChangesAsync(ct);

            if (season is not null)
            {
                var existingLink = await context.SeasonTeams
                    .FirstOrDefaultAsync(st => st.SeasonId == season.Id && st.TeamId == team.Id, ct);

                if (existingLink is null)
                {
                    var seasonTeam = SeasonTeam.Create(season.Id, team.Id);
                    context.SeasonTeams.Add(seasonTeam);
                }
            }
        }

        if (season is not null)
        {
            season.RecordTeamsSync();
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

    public async Task<MatchSyncResult> SyncMatchesForCompetitionAsync(int competitionExternalId, CancellationToken ct = default)
    {
        var competition = await context.Competitions
            .FirstOrDefaultAsync(c => c.ExternalId == competitionExternalId, ct);

        if (competition is null)
        {
            logger.LogWarning("Competition with external ID {ExternalId} not found", competitionExternalId);
            return new MatchSyncResult(competitionExternalId, false);
        }

        var apiMatches = await footballDataService.GetMatchesForCompetitionAsync(
            competitionExternalId, null, null, ct);

        var finishedExternalIds = apiMatches
            .Where(m => string.Equals(m.Status, "FINISHED", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Id)
            .ToHashSet();

        var hasNewlyFinishedMatches = await context.Matches
            .AnyAsync(m => finishedExternalIds.Contains(m.ExternalId)
                        && m.Status != MatchStatus.Finished, ct);

        await ProcessMatchesAsync(apiMatches, competition, ct);
        await context.SaveChangesAsync(ct);

        return new MatchSyncResult(competitionExternalId, hasNewlyFinishedMatches);
    }

    public async Task<StandingsSyncResult> SyncStandingsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default)
    {
        var competition = await context.Competitions
            .FirstOrDefaultAsync(c => c.ExternalId == competitionExternalId, ct);

        if (competition is null)
        {
            logger.LogWarning("Competition with external ID {ExternalId} not found", competitionExternalId);
            return new StandingsSyncResult(competitionExternalId, false);
        }

        var standingsResponse = await footballDataService.GetStandingsAsync(competitionExternalId, ct);
        if (standingsResponse is null)
        {
            logger.LogWarning("Standings response missing for competition {ExternalId}", competitionExternalId);
            return new StandingsSyncResult(competitionExternalId, false);
        }

        var seasonDto = standingsResponse.Season;
        var season = await context.Seasons
            .FirstOrDefaultAsync(s => s.CompetitionId == competition.Id && s.ExternalId == seasonDto.Id, ct);

        var newSeasonDetected = false;
        if (season is null)
        {
            var startYear = seasonDto.StartDate.Year;
            season = Season.Create(
                seasonDto.Id,
                competition.Id,
                startYear,
                seasonDto.StartDate,
                seasonDto.EndDate,
                seasonDto.CurrentMatchday,
                true);

            var currentSeasons = await context.Seasons
                .Where(s => s.CompetitionId == competition.Id && s.IsCurrent)
                .ToListAsync(ct);
            foreach (var current in currentSeasons)
            {
                current.SetAsNotCurrent();
            }

            if (seasonDto.Winner is not null)
            {
                var winner = await context.Teams
                    .FirstOrDefaultAsync(t => t.ExternalId == seasonDto.Winner.Id, ct);
                if (winner is not null)
                {
                    season.SetWinner(winner.Id);
                }
            }

            context.Seasons.Add(season);
            newSeasonDetected = true;
        }
        else
        {
            season.UpdateMatchday(seasonDto.CurrentMatchday);
            season.UpdateDates(seasonDto.StartDate, seasonDto.EndDate);
            if (seasonDto.Winner is not null)
            {
                var winner = await context.Teams
                    .FirstOrDefaultAsync(t => t.ExternalId == seasonDto.Winner.Id, ct);
                if (winner is not null)
                {
                    season.SetWinner(winner.Id);
                }
            }
        }

        await context.SaveChangesAsync(ct);

        foreach (var table in standingsResponse.Standings)
        {
            var type = ParseStandingType(table.Type);
            foreach (var row in table.Table)
            {
                var team = await context.Teams
                    .FirstOrDefaultAsync(t => t.ExternalId == row.Team.Id, ct);

                if (team is null)
                {
                    team = Team.Create(
                        row.Team.Id,
                        row.Team.Name,
                        row.Team.ShortName,
                        null,
                        row.Team.Crest,
                        null,
                        null);
                    context.Teams.Add(team);
                    await context.SaveChangesAsync(ct);
                }

                var standing = await context.FootballStandings
                    .FirstOrDefaultAsync(fs => fs.SeasonId == season.Id
                                               && fs.TeamId == team.Id
                                               && fs.Type == type
                                               && fs.Stage == table.Stage
                                               && fs.Group == table.Group, ct);

                if (standing is null)
                {
                    standing = FootballStanding.Create(
                        season.Id,
                        team.Id,
                        type,
                        table.Stage,
                        table.Group,
                        row.Position,
                        row.PlayedGames,
                        row.Won,
                        row.Draw,
                        row.Lost,
                        row.GoalsFor,
                        row.GoalsAgainst,
                        row.GoalDifference,
                        row.Points,
                        row.Form);
                    context.FootballStandings.Add(standing);
                }
                else
                {
                    standing.Update(
                        row.Position,
                        row.PlayedGames,
                        row.Won,
                        row.Draw,
                        row.Lost,
                        row.GoalsFor,
                        row.GoalsAgainst,
                        row.GoalDifference,
                        row.Points,
                        row.Form);
                }
            }
        }

        season.RecordStandingsSync();
        await context.SaveChangesAsync(ct);

        return new StandingsSyncResult(competitionExternalId, newSeasonDetected);
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
        var season = await GetCurrentSeasonAsync(competition.Id, ct);

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
                    apiMatch.Venue,
                    season?.Id);

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

                if (season is not null && match.SeasonId != season.Id)
                {
                    match.AssignSeason(season.Id);
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
        "EXTRA_TIME" => MatchStatus.ExtraTime,
        "PENALTY_SHOOTOUT" => MatchStatus.PenaltyShootout,
        "FINISHED" => MatchStatus.Finished,
        "POSTPONED" => MatchStatus.Postponed,
        "SUSPENDED" => MatchStatus.Suspended,
        "CANCELLED" => MatchStatus.Cancelled,
        "AWARDED" => MatchStatus.Awarded,
        _ => MatchStatus.Scheduled
    };

    private async Task<Season?> GetCurrentSeasonAsync(Guid competitionId, CancellationToken ct)
    {
        if (context.Seasons is null)
        {
            return null;
        }

        try
        {
            return await context.Seasons
                .FirstOrDefaultAsync(s => s.CompetitionId == competitionId && s.IsCurrent, ct);
        }
        catch (NotSupportedException)
        {
            return context.Seasons
                .FirstOrDefault(s => s.CompetitionId == competitionId && s.IsCurrent);
        }
    }

    private static StandingType ParseStandingType(string type) => type.ToUpperInvariant() switch
    {
        "HOME" => StandingType.Home,
        "AWAY" => StandingType.Away,
        _ => StandingType.Total
    };
}
