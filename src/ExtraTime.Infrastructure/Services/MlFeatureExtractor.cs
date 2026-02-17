using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.ML.Models;
using ExtraTime.Application.Features.ML.Services;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services;

public sealed class MlFeatureExtractor(
    IApplicationDbContext context,
    ITeamFormCalculator teamFormCalculator,
    IHeadToHeadService headToHeadService,
    ILogger<MlFeatureExtractor> logger) : IMlFeatureExtractor
{
    public async Task<MatchFeatures> ExtractFeaturesAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await context.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        if (match is null)
        {
            throw new InvalidOperationException($"Match {matchId} not found.");
        }

        return await ExtractFeaturesInternalAsync(match, cancellationToken);
    }

    public async Task<List<MatchFeatures>> ExtractFeaturesBatchAsync(
        List<Guid> matchIds,
        CancellationToken cancellationToken = default)
    {
        if (matchIds.Count == 0)
        {
            return [];
        }

        var matches = await context.Matches
            .AsNoTracking()
            .Where(m => matchIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var matchesById = matches.ToDictionary(m => m.Id);
        var features = new List<MatchFeatures>(matchIds.Count);

        foreach (var matchId in matchIds)
        {
            if (matchesById.TryGetValue(matchId, out var match))
            {
                features.Add(await ExtractFeaturesInternalAsync(match, cancellationToken));
            }
        }

        return features;
    }

    public async Task<List<(MatchFeatures Features, int ActualHomeScore, int ActualAwayScore)>> GetTrainingDataAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? league = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished && m.HomeScore.HasValue && m.AwayScore.HasValue);

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.MatchDateUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.MatchDateUtc <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(league))
        {
            var leagueTerm = league.Trim();
            query = query.Where(m => m.Competition.Code == leagueTerm || m.Competition.Name == leagueTerm);
        }

        var matches = await query
            .OrderBy(m => m.MatchDateUtc)
            .ToListAsync(cancellationToken);

        var trainingData = new List<(MatchFeatures Features, int ActualHomeScore, int ActualAwayScore)>(matches.Count);

        foreach (var match in matches)
        {
            var features = await ExtractFeaturesInternalAsync(match, cancellationToken);
            trainingData.Add((features, match.HomeScore!.Value, match.AwayScore!.Value));
        }

        return trainingData;
    }

    private async Task<MatchFeatures> ExtractFeaturesInternalAsync(
        Match match,
        CancellationToken cancellationToken)
    {
        var features = new MatchFeatures
        {
            MatchId = match.Id.ToString(),
            HomeTeamId = match.HomeTeamId.ToString(),
            AwayTeamId = match.AwayTeamId.ToString()
        };

        var homeForm = await teamFormCalculator.CalculateFormAsync(
            match.HomeTeamId,
            match.CompetitionId,
            10,
            cancellationToken);
        var awayForm = await teamFormCalculator.CalculateFormAsync(
            match.AwayTeamId,
            match.CompetitionId,
            10,
            cancellationToken);

        PopulateFormFeatures(features, homeForm, awayForm);
        await PopulateHeadToHeadFeaturesAsync(features, match, cancellationToken);
        await PopulateLeagueContextFeaturesAsync(features, match, cancellationToken);
        await PopulateOddsFeaturesAsync(features, match.Id, cancellationToken);
        await PopulateXgFeaturesAsync(features, match, cancellationToken);
        await PopulateEloFeaturesAsync(features, match, cancellationToken);
        await PopulateShotFeaturesAsync(features, match, cancellationToken);
        await PopulateInjuryFeaturesAsync(features, match, cancellationToken);
        await PopulateTemporalFeaturesAsync(features, match, cancellationToken);

        logger.LogDebug("Extracted ML features for match {MatchId}", match.Id);
        return features;
    }

    private static void PopulateFormFeatures(
        MatchFeatures features,
        TeamFormCache homeForm,
        TeamFormCache awayForm)
    {
        var homeRecentMatches = Math.Min(5, Math.Max(homeForm.MatchesPlayed, 1));
        var awayRecentMatches = Math.Min(5, Math.Max(awayForm.MatchesPlayed, 1));

        features.HomeFormPointsLast5 = (float)(homeForm.PointsPerMatch * homeRecentMatches);
        features.HomeGoalsScoredLast5 = (float)(homeForm.GoalsPerMatch * homeRecentMatches);
        features.HomeGoalsConcededLast5 = (float)(homeForm.GoalsConcededPerMatch * homeRecentMatches);
        features.HomeCleanSheetsLast5 = 0f;
        features.HomeWinsLast5 = CountResults(homeForm.RecentForm, 'W');
        features.HomeDrawsLast5 = CountResults(homeForm.RecentForm, 'D');
        features.HomeLossesLast5 = CountResults(homeForm.RecentForm, 'L');
        features.HomeGoalsScoredAvg = (float)homeForm.GoalsPerMatch;
        features.HomeGoalsConcededAvg = (float)homeForm.GoalsConcededPerMatch;
        features.HomePointsPerGame = (float)homeForm.PointsPerMatch;
        features.HomeGoalsScoredAtHomeAvg = homeForm.HomeMatchesPlayed > 0
            ? (float)homeForm.HomeGoalsScored / homeForm.HomeMatchesPlayed
            : 0f;
        features.HomeGoalsConcededAtHomeAvg = homeForm.HomeMatchesPlayed > 0
            ? (float)homeForm.HomeGoalsConceded / homeForm.HomeMatchesPlayed
            : 0f;
        features.HomeWinRateAtHome = (float)homeForm.HomeWinRate;

        features.AwayFormPointsLast5 = (float)(awayForm.PointsPerMatch * awayRecentMatches);
        features.AwayGoalsScoredLast5 = (float)(awayForm.GoalsPerMatch * awayRecentMatches);
        features.AwayGoalsConcededLast5 = (float)(awayForm.GoalsConcededPerMatch * awayRecentMatches);
        features.AwayCleanSheetsLast5 = 0f;
        features.AwayWinsLast5 = CountResults(awayForm.RecentForm, 'W');
        features.AwayDrawsLast5 = CountResults(awayForm.RecentForm, 'D');
        features.AwayLossesLast5 = CountResults(awayForm.RecentForm, 'L');
        features.AwayGoalsScoredAvg = (float)awayForm.GoalsPerMatch;
        features.AwayGoalsConcededAvg = (float)awayForm.GoalsConcededPerMatch;
        features.AwayPointsPerGame = (float)awayForm.PointsPerMatch;
        features.AwayGoalsScoredAwayAvg = awayForm.AwayMatchesPlayed > 0
            ? (float)awayForm.AwayGoalsScored / awayForm.AwayMatchesPlayed
            : 0f;
        features.AwayGoalsConcededAwayAvg = awayForm.AwayMatchesPlayed > 0
            ? (float)awayForm.AwayGoalsConceded / awayForm.AwayMatchesPlayed
            : 0f;
        features.AwayWinRateAway = (float)awayForm.AwayWinRate;
    }

    private async Task PopulateHeadToHeadFeaturesAsync(
        MatchFeatures features,
        Match match,
        CancellationToken cancellationToken)
    {
        var headToHead = await headToHeadService.GetOrCalculateAsync(
            match.HomeTeamId,
            match.AwayTeamId,
            match.CompetitionId,
            cancellationToken);

        var homeStats = headToHead.GetStatsForTeam(match.HomeTeamId);
        var awayStats = headToHead.GetStatsForTeam(match.AwayTeamId);

        features.H2HMatchesPlayed = homeStats.TotalMatches;
        features.H2HHomeWins = homeStats.Wins;
        features.H2HAwayWins = awayStats.Wins;
        features.H2HDraws = homeStats.Draws;
        features.H2HHomeGoalsAvg = homeStats.TotalMatches > 0
            ? (float)homeStats.GoalsFor / homeStats.TotalMatches
            : 0f;
        features.H2HAwayGoalsAvg = awayStats.TotalMatches > 0
            ? (float)awayStats.GoalsFor / awayStats.TotalMatches
            : 0f;
        features.H2HBttsRate = (float)headToHead.BttsRate;
        features.H2HOver2_5Rate = (float)headToHead.Over25Rate;
        features.H2HRecentHomeWins = homeStats.RecentWins;
        features.H2HRecentAwayWins = awayStats.RecentWins;
    }

    private async Task PopulateLeagueContextFeaturesAsync(
        MatchFeatures features,
        Match match,
        CancellationToken cancellationToken)
    {
        var finishedMatches = await context.Matches
            .AsNoTracking()
            .Where(m =>
                m.CompetitionId == match.CompetitionId &&
                m.MatchDateUtc < match.MatchDateUtc &&
                m.Status == MatchStatus.Finished &&
                m.HomeScore.HasValue &&
                m.AwayScore.HasValue)
            .ToListAsync(cancellationToken);

        if (finishedMatches.Count > 0)
        {
            features.LeagueAvgHomeGoals = (float)finishedMatches.Average(m => m.HomeScore!.Value);
            features.LeagueAvgAwayGoals = (float)finishedMatches.Average(m => m.AwayScore!.Value);
            features.LeagueHomeAdvantage = features.LeagueAvgHomeGoals - features.LeagueAvgAwayGoals;
        }

        if (match.SeasonId.HasValue)
        {
            var seasonMatchCount = await context.Matches
                .AsNoTracking()
                .CountAsync(m => m.SeasonId == match.SeasonId, cancellationToken);
            var seasonFinishedMatchCount = await context.Matches
                .AsNoTracking()
                .CountAsync(m => m.SeasonId == match.SeasonId && m.Status == MatchStatus.Finished, cancellationToken);

            if (seasonMatchCount > 0)
            {
                features.SeasonProgress = (float)seasonFinishedMatchCount / seasonMatchCount;
            }

            var standings = await context.FootballStandings
                .AsNoTracking()
                .Where(s =>
                    s.SeasonId == match.SeasonId.Value &&
                    s.Type == StandingType.Total &&
                    (s.TeamId == match.HomeTeamId || s.TeamId == match.AwayTeamId))
                .ToListAsync(cancellationToken);

            var homeStanding = standings.FirstOrDefault(s => s.TeamId == match.HomeTeamId);
            var awayStanding = standings.FirstOrDefault(s => s.TeamId == match.AwayTeamId);

            if (homeStanding is not null)
            {
                features.HomeLeaguePosition = homeStanding.Position;
                features.HomeIsTopHalf = homeStanding.Position <= 10 ? 1f : 0f;
            }

            if (awayStanding is not null)
            {
                features.AwayLeaguePosition = awayStanding.Position;
                features.AwayIsTopHalf = awayStanding.Position <= 10 ? 1f : 0f;
            }

            if (homeStanding is not null && awayStanding is not null)
            {
                features.PositionDifference = features.HomeLeaguePosition - features.AwayLeaguePosition;
            }
        }
    }

    private async Task PopulateOddsFeaturesAsync(
        MatchFeatures features,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var odds = await context.MatchOdds
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.MatchId == matchId, cancellationToken);

        if (odds is null)
        {
            return;
        }

        features.HomeOdds = (float)odds.HomeWinOdds;
        features.DrawOdds = (float)odds.DrawOdds;
        features.AwayOdds = (float)odds.AwayWinOdds;
        features.ImpliedHomeProbability = (float)odds.HomeWinProbability;
        features.ImpliedAwayProbability = (float)odds.AwayWinProbability;
    }

    private async Task PopulateXgFeaturesAsync(
        MatchFeatures features,
        Match match,
        CancellationToken cancellationToken)
    {
        var xgStats = await context.TeamXgStats
            .AsNoTracking()
            .Where(x =>
                x.CompetitionId == match.CompetitionId &&
                (x.TeamId == match.HomeTeamId || x.TeamId == match.AwayTeamId))
            .OrderByDescending(x => x.LastSyncedAt)
            .ToListAsync(cancellationToken);

        var home = xgStats.FirstOrDefault(x => x.TeamId == match.HomeTeamId);
        var away = xgStats.FirstOrDefault(x => x.TeamId == match.AwayTeamId);

        if (home is not null)
        {
            features.HomeXgPerMatch = (float)home.XgPerMatch;
            features.HomeXgAgainstPerMatch = (float)home.XgAgainstPerMatch;
            features.HomeXgOverperformance = (float)home.XgOverperformance;
            features.HomeRecentXgPerMatch = (float)home.RecentXgPerMatch;
        }

        if (away is not null)
        {
            features.AwayXgPerMatch = (float)away.XgPerMatch;
            features.AwayXgAgainstPerMatch = (float)away.XgAgainstPerMatch;
            features.AwayXgOverperformance = (float)away.XgOverperformance;
            features.AwayRecentXgPerMatch = (float)away.RecentXgPerMatch;
        }
    }

    private async Task PopulateEloFeaturesAsync(
        MatchFeatures features,
        Match match,
        CancellationToken cancellationToken)
    {
        var homeElo = await context.TeamEloRatings
            .AsNoTracking()
            .Where(r => r.TeamId == match.HomeTeamId && r.RatingDate <= match.MatchDateUtc)
            .OrderByDescending(r => r.RatingDate)
            .FirstOrDefaultAsync(cancellationToken);
        var awayElo = await context.TeamEloRatings
            .AsNoTracking()
            .Where(r => r.TeamId == match.AwayTeamId && r.RatingDate <= match.MatchDateUtc)
            .OrderByDescending(r => r.RatingDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (homeElo is null || awayElo is null)
        {
            return;
        }

        features.HomeEloRating = (float)homeElo.EloRating;
        features.AwayEloRating = (float)awayElo.EloRating;
        features.EloDifference = features.HomeEloRating - features.AwayEloRating;
    }

    private async Task PopulateShotFeaturesAsync(
        MatchFeatures features,
        Match match,
        CancellationToken cancellationToken)
    {
        var homeStats = await GetAverageShotStatsAsync(match.HomeTeamId, match.CompetitionId, match.MatchDateUtc, cancellationToken);
        var awayStats = await GetAverageShotStatsAsync(match.AwayTeamId, match.CompetitionId, match.MatchDateUtc, cancellationToken);

        features.HomeShotsPerMatch = homeStats.ShotsPerMatch;
        features.HomeShotsOnTargetPerMatch = homeStats.ShotsOnTargetPerMatch;
        features.HomeSOTRatio = homeStats.ShotsToTargetRatio;
        features.AwayShotsPerMatch = awayStats.ShotsPerMatch;
        features.AwayShotsOnTargetPerMatch = awayStats.ShotsOnTargetPerMatch;
        features.AwaySOTRatio = awayStats.ShotsToTargetRatio;
    }

    private async Task PopulateInjuryFeaturesAsync(
        MatchFeatures features,
        Match match,
        CancellationToken cancellationToken)
    {
        var injuryData = await context.TeamInjuries
            .AsNoTracking()
            .Where(i => i.TeamId == match.HomeTeamId || i.TeamId == match.AwayTeamId)
            .ToListAsync(cancellationToken);

        var home = injuryData.FirstOrDefault(i => i.TeamId == match.HomeTeamId);
        var away = injuryData.FirstOrDefault(i => i.TeamId == match.AwayTeamId);

        if (home is not null)
        {
            features.HomeInjuryImpactScore = (float)home.InjuryImpactScore;
            features.HomeKeyPlayersInjured = home.KeyPlayersInjured;
        }

        if (away is not null)
        {
            features.AwayInjuryImpactScore = (float)away.InjuryImpactScore;
            features.AwayKeyPlayersInjured = away.KeyPlayersInjured;
        }
    }

    private async Task PopulateTemporalFeaturesAsync(
        MatchFeatures features,
        Match match,
        CancellationToken cancellationToken)
    {
        features.DayOfWeek = (float)match.MatchDateUtc.DayOfWeek;
        features.IsWeekend = match.MatchDateUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1f : 0f;
        features.Month = match.MatchDateUtc.Month;
        features.DaysSinceLastMatchHome = await GetDaysSinceLastMatchAsync(
            match.HomeTeamId,
            match.CompetitionId,
            match.MatchDateUtc,
            cancellationToken);
        features.DaysSinceLastMatchAway = await GetDaysSinceLastMatchAsync(
            match.AwayTeamId,
            match.CompetitionId,
            match.MatchDateUtc,
            cancellationToken);
    }

    private async Task<float> GetDaysSinceLastMatchAsync(
        Guid teamId,
        Guid competitionId,
        DateTime matchDateUtc,
        CancellationToken cancellationToken)
    {
        var lastMatchDate = await context.Matches
            .AsNoTracking()
            .Where(m =>
                m.CompetitionId == competitionId &&
                m.MatchDateUtc < matchDateUtc &&
                m.Status == MatchStatus.Finished &&
                (m.HomeTeamId == teamId || m.AwayTeamId == teamId))
            .OrderByDescending(m => m.MatchDateUtc)
            .Select(m => (DateTime?)m.MatchDateUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return lastMatchDate.HasValue
            ? (float)Math.Max(0, (matchDateUtc - lastMatchDate.Value).TotalDays)
            : 7f;
    }

    private async Task<ShotAverages> GetAverageShotStatsAsync(
        Guid teamId,
        Guid competitionId,
        DateTime beforeDateUtc,
        CancellationToken cancellationToken)
    {
        var rawStats = await (
            from stat in context.MatchStats.AsNoTracking()
            join match in context.Matches.AsNoTracking() on stat.MatchId equals match.Id
            where match.CompetitionId == competitionId &&
                  match.MatchDateUtc < beforeDateUtc &&
                  match.Status == MatchStatus.Finished &&
                  (match.HomeTeamId == teamId || match.AwayTeamId == teamId)
            orderby match.MatchDateUtc descending
            select new
            {
                match.HomeTeamId,
                match.AwayTeamId,
                stat.HomeShots,
                stat.HomeShotsOnTarget,
                stat.AwayShots,
                stat.AwayShotsOnTarget
            })
            .Take(10)
            .ToListAsync(cancellationToken);

        if (rawStats.Count == 0)
        {
            return ShotAverages.Empty;
        }

        var teamRows = rawStats.Select(s => new
        {
            Shots = s.HomeTeamId == teamId ? s.HomeShots : s.AwayShots,
            ShotsOnTarget = s.HomeTeamId == teamId ? s.HomeShotsOnTarget : s.AwayShotsOnTarget
        }).ToList();

        var shotsPerMatch = (float)teamRows.Average(r => r.Shots ?? 0);
        var shotsOnTargetPerMatch = (float)teamRows.Average(r => r.ShotsOnTarget ?? 0);
        var ratio = shotsPerMatch > 0 ? shotsOnTargetPerMatch / shotsPerMatch : 0f;

        return new ShotAverages(shotsPerMatch, shotsOnTargetPerMatch, ratio);
    }

    private static float CountResults(string form, char result)
    {
        return string.IsNullOrWhiteSpace(form) ? 0f : form.Count(c => c == result);
    }

    private sealed record ShotAverages(float ShotsPerMatch, float ShotsOnTargetPerMatch, float ShotsToTargetRatio)
    {
        public static ShotAverages Empty { get; } = new(0f, 0f, 0f);
    }
}
