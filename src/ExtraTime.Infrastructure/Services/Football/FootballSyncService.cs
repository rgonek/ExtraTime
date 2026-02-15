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
    private const int IncrementalMatchLookbackDays = 2;
    private const int IncrementalMatchLookaheadDays = 14;

    public async Task SyncCompetitionsAsync(CancellationToken ct = default)
    {
        var competitionIds = _settings.SupportedCompetitionIds.Distinct().ToArray();
        logger.LogInformation("Starting competition sync for {Count} competitions: [{Ids}]",
            competitionIds.Length,
            string.Join(", ", competitionIds));

        foreach (var externalId in competitionIds)
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
                var competitionType = ParseCompetitionType(apiCompetition.Type, apiCompetition.Id);
                competition = Competition.Create(
                    apiCompetition.Id,
                    apiCompetition.Name,
                    apiCompetition.Code,
                    apiCompetition.Area.Name,
                    apiCompetition.Emblem,
                    competitionType);
                competition.UpdateCurrentSeason(
                    apiCompetition.CurrentSeason?.CurrentMatchday,
                    apiCompetition.CurrentSeason?.StartDate,
                    apiCompetition.CurrentSeason?.EndDate);
                context.Competitions.Add(competition);
                logger.LogInformation("Added new competition: {Name}", competition.Name);
            }
            else
            {
                var competitionType = ParseCompetitionType(apiCompetition.Type, apiCompetition.Id);
                competition.UpdateDetails(
                    apiCompetition.Name,
                    apiCompetition.Code,
                    apiCompetition.Area.Name,
                    apiCompetition.Emblem,
                    competitionType);
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

        await SyncTeamsForCompetitionAsync(competition.ExternalId, ct);
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
        var teamExternalIds = apiTeams
            .Select(t => t.Id)
            .Distinct()
            .ToList();

        var teamsDict = await context.Teams
            .Where(t => teamExternalIds.Contains(t.ExternalId))
            .ToDictionaryAsync(t => t.ExternalId, ct);

        HashSet<Guid> seasonTeamIds = season is null
            ? []
            : (await context.SeasonTeams
                .Where(st => st.SeasonId == season.Id)
                .Select(st => st.TeamId)
                .ToListAsync(ct))
            .ToHashSet();

        foreach (var apiTeam in apiTeams)
        {
            if (!teamsDict.TryGetValue(apiTeam.Id, out var team))
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
                teamsDict[apiTeam.Id] = team;
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

            if (season is not null)
            {
                if (seasonTeamIds.Add(team.Id))
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
            competitionExternalId,
            new CompetitionMatchesApiFilter(
                DateFrom: Clock.UtcNow.Date.AddDays(-IncrementalMatchLookbackDays),
                DateTo: Clock.UtcNow.Date.AddDays(IncrementalMatchLookaheadDays)),
            ct);

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

            context.Seasons.Add(season);
            newSeasonDetected = true;
        }
        else
        {
            season.UpdateMatchday(seasonDto.CurrentMatchday);
            season.UpdateDates(seasonDto.StartDate, seasonDto.EndDate);
        }

        var teamDtosByExternalId = standingsResponse.Standings
            .SelectMany(s => s.Table)
            .Select(r => r.Team)
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var teamExternalIds = teamDtosByExternalId.Keys.ToList();
        var teamsDict = await context.Teams
            .Where(t => teamExternalIds.Contains(t.ExternalId))
            .ToDictionaryAsync(t => t.ExternalId, ct);

        foreach (var teamDto in teamDtosByExternalId.Values)
        {
            if (teamsDict.ContainsKey(teamDto.Id))
            {
                continue;
            }

            var team = Team.Create(
                teamDto.Id,
                teamDto.Name,
                teamDto.ShortName,
                null,
                teamDto.Crest,
                null,
                null);
            context.Teams.Add(team);
            teamsDict[teamDto.Id] = team;
        }

        if (seasonDto.Winner is not null)
        {
            teamsDict.TryGetValue(seasonDto.Winner.Id, out var winnerTeam);
            winnerTeam ??= await context.Teams
                .FirstOrDefaultAsync(t => t.ExternalId == seasonDto.Winner.Id, ct);

            if (winnerTeam is not null)
            {
                season.SetWinner(winnerTeam.Id);
            }
        }

        static string BuildStandingKey(Guid teamId, StandingType type, string? stage, string? group) =>
            $"{teamId:N}|{type}|{stage ?? string.Empty}|{group ?? string.Empty}";

        var standingsLookup = (await context.FootballStandings
                .Where(fs => fs.SeasonId == season.Id)
                .ToListAsync(ct))
            .ToDictionary(
                fs => BuildStandingKey(fs.TeamId, fs.Type, fs.Stage, fs.Group),
                fs => fs);

        foreach (var table in standingsResponse.Standings)
        {
            var type = ParseStandingType(table.Type, competitionExternalId);
            foreach (var row in table.Table)
            {
                if (!teamsDict.TryGetValue(row.Team.Id, out var team))
                {
                    logger.LogWarning("Team {TeamExternalId} not found when syncing standings", row.Team.Id);
                    continue;
                }

                var standingKey = BuildStandingKey(team.Id, type, table.Stage, table.Group);
                standingsLookup.TryGetValue(standingKey, out var standing);

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
                    standingsLookup[standingKey] = standing;
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
                var newStatus = ParseMatchStatus(apiMatch.Status, apiMatch.Id);

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
        var teamExternalIds = apiMatches
            .SelectMany(m => new[] { m.HomeTeam.Id, m.AwayTeam.Id })
            .Distinct()
            .ToList();
        var matchExternalIds = apiMatches
            .Select(m => m.Id)
            .Distinct()
            .ToList();

        var teamsDict = await context.Teams
            .Where(t => teamExternalIds.Contains(t.ExternalId))
            .ToDictionaryAsync(t => t.ExternalId, ct);
        var matchesDict = await context.Matches
            .Where(m => matchExternalIds.Contains(m.ExternalId))
            .ToDictionaryAsync(m => m.ExternalId, ct);

        foreach (var apiMatch in apiMatches)
        {
            teamsDict.TryGetValue(apiMatch.HomeTeam.Id, out var homeTeam);
            teamsDict.TryGetValue(apiMatch.AwayTeam.Id, out var awayTeam);

            if (homeTeam is null || awayTeam is null)
            {
                logger.LogWarning("Teams not found for match {MatchId}. Home: {HomeId}, Away: {AwayId}",
                    apiMatch.Id, apiMatch.HomeTeam.Id, apiMatch.AwayTeam.Id);
                continue;
            }

            matchesDict.TryGetValue(apiMatch.Id, out var match);

            if (match is null)
            {
                match = Match.Create(
                    apiMatch.Id,
                    competition.Id,
                    homeTeam.Id,
                    awayTeam.Id,
                    apiMatch.UtcDate,
                    ParseMatchStatus(apiMatch.Status, apiMatch.Id),
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
                matchesDict[apiMatch.Id] = match;
                logger.LogInformation("Added new match: {Home} vs {Away} on {Date}",
                    homeTeam.Name, awayTeam.Name, match.MatchDateUtc);
            }
            else
            {
                var previousStatus = match.Status;
                var newStatus = ParseMatchStatus(apiMatch.Status, apiMatch.Id);

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

    private MatchStatus ParseMatchStatus(string? status, int matchExternalId)
    {
        var parsedStatus = status?.ToUpperInvariant() switch
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
            _ => (MatchStatus?)null
        };

        if (parsedStatus.HasValue)
        {
            return parsedStatus.Value;
        }

        logger.LogWarning(
            "Unknown match status {Status} for external match {MatchExternalId}, defaulting to {DefaultStatus}",
            status,
            matchExternalId,
            MatchStatus.Scheduled);
        return MatchStatus.Scheduled;
    }

    private CompetitionType ParseCompetitionType(string? type, int competitionExternalId)
    {
        var parsedType = type?.ToUpperInvariant() switch
        {
            "LEAGUE" => CompetitionType.League,
            "LEAGUE_CUP" => CompetitionType.LeagueCup,
            "CUP" => CompetitionType.Cup,
            "PLAYOFFS" => CompetitionType.Playoffs,
            _ => (CompetitionType?)null
        };

        if (parsedType.HasValue)
        {
            return parsedType.Value;
        }

        logger.LogWarning(
            "Unknown competition type {Type} for external competition {CompetitionExternalId}, defaulting to {DefaultType}",
            type,
            competitionExternalId,
            CompetitionType.League);
        return CompetitionType.League;
    }

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

    private StandingType ParseStandingType(string? type, int competitionExternalId)
    {
        var parsedType = type?.ToUpperInvariant() switch
        {
            "HOME" => StandingType.Home,
            "AWAY" => StandingType.Away,
            "TOTAL" => StandingType.Total,
            _ => (StandingType?)null
        };

        if (parsedType.HasValue)
        {
            return parsedType.Value;
        }

        logger.LogWarning(
            "Unknown standing type {Type} for external competition {CompetitionExternalId}, defaulting to {DefaultType}",
            type,
            competitionExternalId,
            StandingType.Total);
        return StandingType.Total;
    }
}
