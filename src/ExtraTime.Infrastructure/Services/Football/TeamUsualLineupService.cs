using System.Text.Json;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class TeamUsualLineupService(
    IApplicationDbContext context,
    ILogger<TeamUsualLineupService> logger) : ITeamUsualLineupService
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(3);

    public async Task<TeamUsualLineup> GetOrCalculateAsync(
        Guid teamId,
        Guid seasonId,
        int matchesToAnalyze = 10,
        CancellationToken cancellationToken = default)
    {
        var cached = await context.TeamUsualLineups
            .FirstOrDefaultAsync(
                t => t.TeamId == teamId && t.SeasonId == seasonId,
                cancellationToken);

        if (cached is not null && (Clock.UtcNow - cached.CalculatedAt) < CacheExpiry)
        {
            return cached;
        }

        return await CalculateAndStoreAsync(
            teamId,
            seasonId,
            matchesToAnalyze,
            cached,
            cancellationToken);
    }

    private async Task<TeamUsualLineup> CalculateAndStoreAsync(
        Guid teamId,
        Guid seasonId,
        int matchesToAnalyze,
        TeamUsualLineup? existing,
        CancellationToken cancellationToken)
    {
        var lineups = await context.MatchLineups
            .Include(ml => ml.Match)
            .Where(ml => ml.TeamId == teamId && ml.Match.SeasonId == seasonId)
            .Where(ml => ml.Match.Status == MatchStatus.Finished)
            .OrderByDescending(ml => ml.Match.MatchDateUtc)
            .Take(matchesToAnalyze)
            .ToListAsync(cancellationToken);

        if (lineups.Count == 0)
        {
            var empty = existing ?? TeamUsualLineup.Create(
                teamId,
                seasonId,
                null,
                "[]",
                "[]",
                "[]",
                "[]",
                null,
                0);

            if (existing is null)
            {
                context.TeamUsualLineups.Add(empty);
                await context.SaveChangesAsync(cancellationToken);
            }

            return empty;
        }

        var topFormation = lineups
            .Where(l => !string.IsNullOrWhiteSpace(l.Formation))
            .GroupBy(l => l.Formation)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?
            .Key;

        var allPlayers = lineups.SelectMany(l => l.GetStartingPlayers()).ToList();

        var goalkeepers = GetUsualPlayersByPosition(allPlayers, "Goalkeeper", "GK");
        var defenders = GetUsualPlayersByPosition(
            allPlayers,
            "Defender",
            "Centre-Back",
            "Left-Back",
            "Right-Back",
            "DEF");
        var midfielders = GetUsualPlayersByPosition(
            allPlayers,
            "Midfielder",
            "Central Midfield",
            "Defensive Midfield",
            "Attacking Midfield",
            "Left Midfield",
            "Right Midfield",
            "MID");
        var forwards = GetUsualPlayersByPosition(
            allPlayers,
            "Forward",
            "Attacker",
            "Centre-Forward",
            "Left Winger",
            "Right Winger",
            "FWD");

        var topCaptain = lineups
            .Where(l => !string.IsNullOrWhiteSpace(l.CaptainName))
            .GroupBy(l => l.CaptainName)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?
            .Key;

        var gkJson = JsonSerializer.Serialize(goalkeepers);
        var defJson = JsonSerializer.Serialize(defenders);
        var midJson = JsonSerializer.Serialize(midfielders);
        var fwdJson = JsonSerializer.Serialize(forwards);

        if (existing is not null)
        {
            existing.Update(topFormation, gkJson, defJson, midJson, fwdJson, topCaptain, lineups.Count);
        }
        else
        {
            existing = TeamUsualLineup.Create(
                teamId,
                seasonId,
                topFormation,
                gkJson,
                defJson,
                midJson,
                fwdJson,
                topCaptain,
                lineups.Count);
            context.TeamUsualLineups.Add(existing);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Calculated usual lineup for team {TeamId} season {SeasonId}. Matches analyzed: {MatchCount}",
            teamId,
            seasonId,
            lineups.Count);

        return existing;
    }

    private static List<UsualPlayer> GetUsualPlayersByPosition(
        List<LineupPlayer> players,
        params string[] positions)
    {
        return players
            .Where(player => positions.Any(position =>
                player.Position?.Contains(position, StringComparison.OrdinalIgnoreCase) == true))
            .GroupBy(player => new { player.Id, player.Name, player.Position })
            .Select(group => new UsualPlayer(
                group.Key.Id,
                group.Key.Name,
                group.Key.Position,
                group.Count()))
            .OrderByDescending(player => player.Appearances)
            .ToList();
    }
}
