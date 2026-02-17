using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class HeadToHeadService(
    IApplicationDbContext context,
    ILogger<HeadToHeadService> logger) : IHeadToHeadService
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(7);

    public async Task<HeadToHead> GetOrCalculateAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default)
    {
        if (team1Id == team2Id)
            throw new ArgumentException("Team IDs must be different", nameof(team2Id));

        var (first, second) = OrderTeamIds(team1Id, team2Id);
        var cached = await GetRecordAsync(first, second, competitionId, cancellationToken);

        if (cached != null && (Clock.UtcNow - cached.CalculatedAt) < CacheExpiry)
            return cached;

        return await CalculateAndStoreAsync(first, second, competitionId, cached, cancellationToken);
    }

    public async Task<HeadToHead> RefreshAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default)
    {
        if (team1Id == team2Id)
            throw new ArgumentException("Team IDs must be different", nameof(team2Id));

        var (first, second) = OrderTeamIds(team1Id, team2Id);
        var cached = await GetRecordAsync(first, second, competitionId, cancellationToken);

        return await CalculateAndStoreAsync(first, second, competitionId, cached, cancellationToken);
    }

    private static (Guid Team1Id, Guid Team2Id) OrderTeamIds(Guid team1Id, Guid team2Id)
    {
        return team1Id.CompareTo(team2Id) < 0
            ? (team1Id, team2Id)
            : (team2Id, team1Id);
    }

    private async Task<HeadToHead?> GetRecordAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId,
        CancellationToken cancellationToken)
    {
        return await context.HeadToHeads
            .Include(h => h.Team1)
            .Include(h => h.Team2)
            .FirstOrDefaultAsync(
                h => h.Team1Id == team1Id &&
                     h.Team2Id == team2Id &&
                     h.CompetitionId == competitionId,
                cancellationToken);
    }

    private async Task<HeadToHead> CalculateAndStoreAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId,
        HeadToHead? existing,
        CancellationToken cancellationToken)
    {
        var matchesQuery = context.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Where(m =>
                (m.HomeTeamId == team1Id && m.AwayTeamId == team2Id) ||
                (m.HomeTeamId == team2Id && m.AwayTeamId == team1Id));

        if (competitionId.HasValue)
        {
            matchesQuery = matchesQuery.Where(m => m.CompetitionId == competitionId.Value);
        }

        var matches = await matchesQuery
            .OrderByDescending(m => m.MatchDateUtc)
            .ToListAsync(cancellationToken);

        var scoredMatches = matches
            .Where(m => m.HomeScore.HasValue && m.AwayScore.HasValue)
            .ToList();

        var h2h = existing ?? HeadToHead.Create(team1Id, team2Id, competitionId);

        var totalMatches = 0;
        var team1Wins = 0;
        var team2Wins = 0;
        var draws = 0;
        var team1Goals = 0;
        var team2Goals = 0;
        var team1HomeMatches = 0;
        var team1HomeWins = 0;
        var team1HomeGoals = 0;
        var team1HomeConceded = 0;
        var bothTeamsScoredCount = 0;
        var over25Count = 0;

        foreach (var match in scoredMatches)
        {
            totalMatches++;

            var team1IsHome = match.HomeTeamId == team1Id;
            var t1Score = team1IsHome ? match.HomeScore!.Value : match.AwayScore!.Value;
            var t2Score = team1IsHome ? match.AwayScore!.Value : match.HomeScore!.Value;

            team1Goals += t1Score;
            team2Goals += t2Score;

            if (t1Score > t2Score)
            {
                team1Wins++;
            }
            else if (t2Score > t1Score)
            {
                team2Wins++;
            }
            else
            {
                draws++;
            }

            if (team1IsHome)
            {
                team1HomeMatches++;
                team1HomeGoals += t1Score;
                team1HomeConceded += t2Score;

                if (t1Score > t2Score)
                {
                    team1HomeWins++;
                }
            }

            if (t1Score > 0 && t2Score > 0)
            {
                bothTeamsScoredCount++;
            }

            if (t1Score + t2Score > 2)
            {
                over25Count++;
            }
        }

        var recentMatches = scoredMatches.Take(3).ToList();
        var recentMatchesCount = recentMatches.Count;
        var recentTeam1Wins = 0;
        var recentTeam2Wins = 0;
        var recentDraws = 0;

        foreach (var recentMatch in recentMatches)
        {
            var team1IsHome = recentMatch.HomeTeamId == team1Id;
            var t1Score = team1IsHome ? recentMatch.HomeScore!.Value : recentMatch.AwayScore!.Value;
            var t2Score = team1IsHome ? recentMatch.AwayScore!.Value : recentMatch.HomeScore!.Value;

            if (t1Score > t2Score)
            {
                recentTeam1Wins++;
            }
            else if (t2Score > t1Score)
            {
                recentTeam2Wins++;
            }
            else
            {
                recentDraws++;
            }
        }

        var lastMatch = scoredMatches.FirstOrDefault();

        h2h.UpdateStats(
            totalMatches,
            team1Wins,
            team2Wins,
            draws,
            team1Goals,
            team2Goals,
            team1HomeMatches,
            team1HomeWins,
            team1HomeGoals,
            team1HomeConceded,
            bothTeamsScoredCount,
            over25Count,
            lastMatch?.MatchDateUtc,
            lastMatch?.Id,
            recentMatchesCount,
            recentTeam1Wins,
            recentTeam2Wins,
            recentDraws,
            totalMatches);

        if (existing == null)
        {
            context.HeadToHeads.Add(h2h);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Calculated H2H: {Team1Id} vs {Team2Id} ({CompetitionId}) = {Matches} matches",
            team1Id,
            team2Id,
            competitionId,
            totalMatches);

        return await GetRecordAsync(team1Id, team2Id, competitionId, cancellationToken) ?? h2h;
    }
}
