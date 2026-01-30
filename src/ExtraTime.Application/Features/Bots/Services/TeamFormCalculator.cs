using System.Text;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Application.Features.Bots.Services;

public sealed class TeamFormCalculator(
    IApplicationDbContext context,
    TimeProvider timeProvider,
    ILogger<TeamFormCalculator> logger) : ITeamFormCalculator
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(6);

    public async Task<TeamFormCache> CalculateFormAsync(
        Guid teamId,
        Guid competitionId,
        int matchesAnalyzed = 5,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var cached = await GetCachedFormAsync(teamId, competitionId, cancellationToken);
        if (cached != null && (now - cached.CalculatedAt) < CacheExpiry)
        {
            return cached;
        }

        var matches = await context.Matches
            .Where(m => m.CompetitionId == competitionId)
            .Where(m => m.Status == MatchStatus.Finished)
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .OrderByDescending(m => m.MatchDateUtc)
            .Take(matchesAnalyzed)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
        {
            return CreateDefaultForm(teamId, competitionId, matchesAnalyzed, now);
        }

        var form = CalculateStats(teamId, competitionId, matches, matchesAnalyzed, now);

        if (cached != null)
        {
            UpdateFormCache(cached, form);
        }
        else
        {
            context.TeamFormCaches.Add(form);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Calculated form for team {TeamId}: {Form} ({Points} PPM)",
            teamId, form.RecentForm, form.PointsPerMatch);

        return form;
    }

    private TeamFormCache CalculateStats(
        Guid teamId,
        Guid competitionId,
        List<Match> matches,
        int matchesAnalyzed,
        DateTime now)
    {
        var form = new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            CompetitionId = competitionId,
            MatchesAnalyzed = matchesAnalyzed,
            CalculatedAt = now,
            LastMatchDate = matches.FirstOrDefault()?.MatchDateUtc
        };

        var recentFormBuilder = new StringBuilder();
        int currentStreak = 0;
        bool streakType = true;

        foreach (var match in matches)
        {
            bool isHome = match.HomeTeamId == teamId;
            int goalsFor = isHome ? match.HomeScore!.Value : match.AwayScore!.Value;
            int goalsAgainst = isHome ? match.AwayScore!.Value : match.HomeScore!.Value;

            form.MatchesPlayed++;
            form.GoalsScored += goalsFor;
            form.GoalsConceded += goalsAgainst;

            if (isHome)
            {
                form.HomeMatchesPlayed++;
                form.HomeGoalsScored += goalsFor;
                form.HomeGoalsConceded += goalsAgainst;
            }
            else
            {
                form.AwayMatchesPlayed++;
                form.AwayGoalsScored += goalsFor;
                form.AwayGoalsConceded += goalsAgainst;
            }

            char result;
            if (goalsFor > goalsAgainst)
            {
                form.Wins++;
                if (isHome) form.HomeWins++;
                else form.AwayWins++;
                result = 'W';

                if (recentFormBuilder.Length == 0) { currentStreak = 1; streakType = true; }
                else if (streakType) currentStreak++;
            }
            else if (goalsFor < goalsAgainst)
            {
                form.Losses++;
                result = 'L';

                if (recentFormBuilder.Length == 0) { currentStreak = -1; streakType = false; }
                else if (!streakType) currentStreak--;
            }
            else
            {
                form.Draws++;
                result = 'D';
                currentStreak = 0;
            }

            recentFormBuilder.Append(result);
        }

        if (form.MatchesPlayed > 0)
        {
            form.PointsPerMatch = (form.Wins * 3.0 + form.Draws) / form.MatchesPlayed;
            form.GoalsPerMatch = (double)form.GoalsScored / form.MatchesPlayed;
            form.GoalsConcededPerMatch = (double)form.GoalsConceded / form.MatchesPlayed;
        }

        if (form.HomeMatchesPlayed > 0)
        {
            form.HomeWinRate = (double)form.HomeWins / form.HomeMatchesPlayed;
        }

        if (form.AwayMatchesPlayed > 0)
        {
            form.AwayWinRate = (double)form.AwayWins / form.AwayMatchesPlayed;
        }

        form.CurrentStreak = currentStreak;
        form.RecentForm = recentFormBuilder.ToString();

        return form;
    }

    private TeamFormCache CreateDefaultForm(
        Guid teamId,
        Guid competitionId,
        int matchesAnalyzed,
        DateTime now)
    {
        return new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            CompetitionId = competitionId,
            MatchesAnalyzed = matchesAnalyzed,
            MatchesPlayed = 0,
            PointsPerMatch = 1.0,
            GoalsPerMatch = 1.5,
            GoalsConcededPerMatch = 1.5,
            HomeWinRate = 0.45,
            AwayWinRate = 0.30,
            RecentForm = "",
            CalculatedAt = now
        };
    }

    private void UpdateFormCache(TeamFormCache existing, TeamFormCache updated)
    {
        existing.MatchesPlayed = updated.MatchesPlayed;
        existing.Wins = updated.Wins;
        existing.Draws = updated.Draws;
        existing.Losses = updated.Losses;
        existing.GoalsScored = updated.GoalsScored;
        existing.GoalsConceded = updated.GoalsConceded;
        existing.HomeMatchesPlayed = updated.HomeMatchesPlayed;
        existing.HomeWins = updated.HomeWins;
        existing.HomeGoalsScored = updated.HomeGoalsScored;
        existing.HomeGoalsConceded = updated.HomeGoalsConceded;
        existing.AwayMatchesPlayed = updated.AwayMatchesPlayed;
        existing.AwayWins = updated.AwayWins;
        existing.AwayGoalsScored = updated.AwayGoalsScored;
        existing.AwayGoalsConceded = updated.AwayGoalsConceded;
        existing.PointsPerMatch = updated.PointsPerMatch;
        existing.GoalsPerMatch = updated.GoalsPerMatch;
        existing.GoalsConcededPerMatch = updated.GoalsConcededPerMatch;
        existing.HomeWinRate = updated.HomeWinRate;
        existing.AwayWinRate = updated.AwayWinRate;
        existing.CurrentStreak = updated.CurrentStreak;
        existing.RecentForm = updated.RecentForm;
        existing.MatchesAnalyzed = updated.MatchesAnalyzed;
        existing.CalculatedAt = updated.CalculatedAt;
        existing.LastMatchDate = updated.LastMatchDate;
    }

    public async Task<TeamFormCache?> GetCachedFormAsync(
        Guid teamId,
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamFormCaches
            .FirstOrDefaultAsync(
                t => t.TeamId == teamId && t.CompetitionId == competitionId,
                cancellationToken);
    }

    public async Task RefreshAllFormCachesAsync(CancellationToken cancellationToken = default)
    {
        var teamCompetitions = await context.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Select(m => new { m.HomeTeamId, m.CompetitionId })
            .Union(context.Matches
                .Where(m => m.Status == MatchStatus.Finished)
                .Select(m => new { HomeTeamId = m.AwayTeamId, m.CompetitionId }))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var tc in teamCompetitions)
        {
            await CalculateFormAsync(tc.HomeTeamId, tc.CompetitionId, 5, cancellationToken);
        }

        logger.LogInformation("Refreshed form caches for {Count} team-competition pairs", teamCompetitions.Count);
    }
}
